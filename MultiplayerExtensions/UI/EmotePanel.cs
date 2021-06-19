﻿using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities;
using MultiplayerExtensions.Emotes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zenject;
using static BeatSaberMarkupLanguage.Components.CustomListTableData;

namespace MultiplayerExtensions.UI
{
    public class EmotePanel : IInitializable
    {
        private FloatingScreen floatingScreen = null!;
        private Vector3 screenPosition;
        private Vector3 screenAngles;

        private readonly string IMAGES_PATH = Path.Combine(UnityGame.UserDataPath, nameof(MultiplayerExtensions), "Emotes");
        private bool parsed;
        private Dictionary<string, EmoteImage> emoteImages = null!;


        [UIComponent("emote-list")]
        public CustomListTableData customListTableData = null!;

        public void Initialize()
        {
            parsed = false;
            Directory.CreateDirectory(IMAGES_PATH);
            emoteImages = new Dictionary<string, EmoteImage>();
        }

        private void Parse()
        {
            if (!parsed)
            {
                parsed = true;
                floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(75, 30), true, new Vector3(0, 0.25f, 1), new Quaternion(0, 0, 0, 0));
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "MultiplayerExtensions.UI.EmotePanel.bsml"), floatingScreen.gameObject, this);
                floatingScreen.gameObject.SetActive(false);
                floatingScreen.gameObject.name = "MultiplayerEmotePanel";
                floatingScreen.transform.localEulerAngles = new Vector3(50, 0);
                screenPosition = floatingScreen.transform.position;
                screenAngles = floatingScreen.transform.localEulerAngles;
            }
            // Restore position so it respawns where we expect it to
            floatingScreen.transform.position = screenPosition;
            floatingScreen.transform.localEulerAngles = screenAngles;
        }

        internal void ToggleActive()
        {
            Parse();
            if (floatingScreen.gameObject.activeSelf)
            {
                floatingScreen.gameObject.SetActive(false);
            }
            else
            {
                floatingScreen.gameObject.SetActive(true);
                ShowImages();
            }
        }

        private void LoadImages()
        {
            foreach (var imageToDelete in emoteImages.Where(coverImage => !File.Exists(coverImage.Key)).ToList())
            {
                emoteImages.Remove(imageToDelete.Key);
            }

            string[] ext = { "jpg", "png" };
            IEnumerable<string> imageFiles = Directory.EnumerateFiles(IMAGES_PATH, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));

            foreach (var file in imageFiles)
            {
                if (!emoteImages.ContainsKey(file))
                {
                    emoteImages.Add(file, new EmoteImage(file));
                }
            }
        }

        private void ShowImages()
        {
            customListTableData.data.Clear();

            LoadImages();
            foreach (var emoteImage in emoteImages)
            {
                if (!emoteImage.Value.SpriteWasLoaded && !emoteImage.Value.Blacklist)
                {
                    emoteImage.Value.SpriteLoaded += CoverImage_SpriteLoaded;
                    _ = emoteImage.Value.Sprite;
                }
                else if (emoteImage.Value.SpriteWasLoaded)
                {
                    customListTableData.data.Add(new CustomCellInfo(emoteImage.Key, "", emoteImage.Value.Sprite));
                }
            }
            customListTableData.tableView.ReloadData();
            customListTableData.tableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
        }

        private void CoverImage_SpriteLoaded(object sender, EventArgs e)
        {
            if (sender is EmoteImage emoteImage)
            {
                if (emoteImage.SpriteWasLoaded)
                {
                    customListTableData.data.Add(new CustomCellInfo(Path.GetFileName(emoteImage.Path), emoteImage.Path, emoteImage.Sprite));
                    customListTableData.tableView.ReloadData();
                }
                emoteImage.SpriteLoaded -= CoverImage_SpriteLoaded;
            }
        }

        [UIAction("close-screen")]
        internal void CloseScreen() => floatingScreen.gameObject.SetActive(false);
    }
}
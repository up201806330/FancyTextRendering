﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JimmysUnityUtilities;
using LogicUI.FancyTextRendering.MarkdownLogic;
using TMPro;
using UnityEngine;

namespace LogicUI.FancyTextRendering
{
    /// <summary>
    /// Converts markdown into TMP rich text tags.
    /// Very unfinished and experimental. Not even close to being a complete markdown renderer.
    /// </summary>
    public static class Markdown
    {
        // Useful links for anyone who wants to finish this
        // http://digitalnativestudios.com/textmeshpro/docs/rich-text/
        // https://github.com/adam-p/markdown-here/wiki/Markdown-Cheatsheet


        public static void RenderToTextMesh(string markdownSource, TMP_Text textMesh)
            => RenderToTextMesh(markdownSource, textMesh, MarkdownRenderingSettings.Default);

        public static void RenderToTextMesh(string markdownSource, TMP_Text textMesh, MarkdownRenderingSettings settings)
        {
            string richText = MarkdownToRichText(markdownSource, settings);

            textMesh.text = richText;
            UpdateTextMesh(textMesh);
        }

        public static void UpdateTextMesh(TMP_Text textMesh)
        {
            ResetLinkInfo(); // TextMeshPro doesn't reset the link infos automatically, so we have to do it manually in situations where it will be changed

            textMesh.ForceMeshUpdate();
            textMesh.GetComponent<TextLinkHelper>()?.LinkDataUpdated();


            void ResetLinkInfo()
            {
                if (textMesh.textInfo != null) // Make sure the text is initialized; required as of TMP 2.1
                {
                    textMesh.textInfo.linkInfo = Array.Empty<TMP_LinkInfo>();
                    textMesh.textInfo.linkCount = 0;
                }
            }
        }


        public static string MarkdownToRichText(string source)
            => MarkdownToRichText(source, MarkdownRenderingSettings.Default);

        public static string MarkdownToRichText(string source, MarkdownRenderingSettings settings)
        {
            if (source.IsNullOrEmpty())
                return String.Empty;


            var lines = new List<MarkdownLine>();

            using (var reader = new StringReader(source))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(new MarkdownLine()
                    {
                        Builder = new StringBuilder(line)
                    });
                }
            }


            foreach (var processor in LineProcessors)
                processor.Process(lines, settings);


            var builder = new StringBuilder();

            foreach (var line in lines)
            {
                if (!line.DeleteLineAfterProcessing)
                    builder.AppendLine(line.Finish());
            }

            return builder.ToString();
        }


        private static readonly IReadOnlyList<MarkdownLineProcessorBase> LineProcessors = new MarkdownLineProcessorBase[]
        {
            new AutoLinksHttp(),
            new AutoLinksHttps(),
            new UnorderedLists(),
            new OrderedLists(),
            new Bold(),
            new Italics(),
            new Strikethrough(),
            new Monospace(),
            new Headers(),
            new Links(),
            new Superscript(),
            new Subscript(),
        };
    }
}
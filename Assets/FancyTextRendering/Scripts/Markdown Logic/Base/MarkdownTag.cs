﻿using System;
using System.Collections.Generic;
using System.Text;
using JimmysUnityUtilities;
using UnityEngine;

namespace LogicUI.FancyTextRendering.MarkdownLogic
{
    internal abstract class SimpleMarkdownTag : MarkdownTag
    {
        protected abstract string MarkdownIndicator { get; }
        protected abstract string RichTextOpenTag { get; }
        protected abstract string RichTextCloseTag { get; }

        protected override string GetMarkdownIndicator(MarkdownRenderingSettings _) => MarkdownIndicator;
        protected override string GetRichTextOpenTag(MarkdownRenderingSettings _) => RichTextOpenTag;
        protected override string GetRichTextCloseTag(MarkdownRenderingSettings _) => RichTextCloseTag;
    }

    internal abstract class MarkdownTag : MarkdownLineProcessorBase
    {
        protected abstract string GetMarkdownIndicator(MarkdownRenderingSettings settings);
        protected abstract string GetRichTextOpenTag(MarkdownRenderingSettings settings);
        protected abstract string GetRichTextCloseTag(MarkdownRenderingSettings settings);

        protected virtual char? IgnoreContents { get; } = null;

        protected override void ProcessInternal(IReadOnlyList<MarkdownLine> lines, MarkdownRenderingSettings settings)
        {
            // It's more efficient to get these values once than for every line. That's why this class inherits from
            // MarkdownLineProcessorBase instead of SimpleMarkdownLineProcessor.

            string markdownIndicator = GetMarkdownIndicator(settings);
            string richTextOpenTag = GetRichTextOpenTag(settings);
            string richTextCloseTag = GetRichTextCloseTag(settings);

            foreach (MarkdownLine line in lines)
            {
                if (line.DisableFutureProcessing)
                    continue;

                ProcessLine(line);
            }


            void ProcessLine(MarkdownLine line)
            {
                var lineBuilder = line.Builder;

                int index = 0;
                while (index < lineBuilder.Length - markdownIndicator.Length * 2)
                {
                    int openTagIndex = GetNextValidTagIndex(index);
                    int closeTagIndex = -1;
                    
                    if (openTagIndex > -1)
                        closeTagIndex = GetNextValidTagIndex(openTagIndex + markdownIndicator.Length);

                    if (closeTagIndex > -1) // If there's a remaining tag pair in the line
                    {
                        if (openTagIndex + markdownIndicator.Length == closeTagIndex)
                        {
                            // Tags have to actually apply to some text. You can write '**' and it doesn't just turn invisible, it shows the asterisks.
                            index = openTagIndex + 1;
                        }
                        else if (IsSpaceBetweenIndexesInvalidForTagging(startIndex: openTagIndex + markdownIndicator.Length, endIndex: closeTagIndex - 1))
                        {
                            index = openTagIndex + 1; ;
                        }
                        else
                        {
                            lineBuilder.ReplaceFirst(markdownIndicator, richTextOpenTag, openTagIndex);
                            lineBuilder.ReplaceFirst(markdownIndicator, richTextCloseTag, out int richTextCloseTagStartIndex, openTagIndex + richTextOpenTag.Length - 1);

                            index = richTextCloseTagStartIndex + richTextCloseTag.Length;
                        }
                    }
                    else
                    {
                        break;
                    }
                }


                int GetNextValidTagIndex(int startIndex)
                {
                    return line.UnescapedIndexOf(markdownIndicator, startIndex);
                }

                bool IsSpaceBetweenIndexesInvalidForTagging(int startIndex, int endIndex)
                {
                    // First, we have to check that the text doesn't begin with the markdown indicator. To solve conflicts, we must always use the last
                    // markdown indicator in a chain. I.e.: in *****example*** we should use the last two asterisks from the first clump as the bold
                    // indicator.
                    if (lineBuilder[startIndex] == markdownIndicator[0])
                        return true;

                    // You can put spaces between stuff to make it not apply. I.e. '* example*' is not italicized.
                    if (lineBuilder[startIndex].IsWhitespace() || lineBuilder[endIndex].IsWhitespace())
                        return true;

                    // Many tags have a defined character that is ignored. So for example you can write "~~~~~" and it's five tildes, not one crossed out tilde.
                    // Furthermore, we dont' tag just escape characters.
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        if (lineBuilder[i] == IgnoreContents)
                            continue;

                        if (lineBuilder[i] == MarkdownLine.EscapeCharacater)
                            continue;

                        return false;
                    }

                    return true;
                }
            }
        }
    }
    
    internal abstract class SimpleAsymmetricMarkdownTag : AsymmetricMarkdownTag
    {
        protected abstract string MarkdownIndicator { get; }
        protected abstract string RichTextOpenTag { get; }
        protected abstract string RichTextCloseTag { get; }

        protected override string GetMarkdownIndicator(MarkdownRenderingSettings _) => MarkdownIndicator;
        protected override string GetRichTextOpenTag(MarkdownRenderingSettings _) => RichTextOpenTag;
        protected override string GetRichTextCloseTag(MarkdownRenderingSettings _) => RichTextCloseTag;
    }

    // Couldn't find the actual name of these "space terminated" tags, TODO change to correct name
    internal abstract class AsymmetricMarkdownTag : MarkdownLineProcessorBase
    {
        protected abstract string GetMarkdownIndicator(MarkdownRenderingSettings settings);
        protected abstract string GetRichTextOpenTag(MarkdownRenderingSettings settings);
        protected abstract string GetRichTextCloseTag(MarkdownRenderingSettings settings);

        protected virtual char? IgnoreContents { get; } = null;

        protected override void ProcessInternal(IReadOnlyList<MarkdownLine> lines, MarkdownRenderingSettings settings)
        {
            // It's more efficient to get these values once than for every line. That's why this class inherits from
            // MarkdownLineProcessorBase instead of SimpleMarkdownLineProcessor.

            string markdownIndicator = GetMarkdownIndicator(settings);
            string richTextOpenTag = GetRichTextOpenTag(settings);
            string richTextCloseTag = GetRichTextCloseTag(settings);

            foreach (MarkdownLine line in lines)
            {
                if (line.DisableFutureProcessing)
                    continue;

                ProcessLine(line);
            }


            void ProcessLine(MarkdownLine line)
            {
                var lineBuilder = line.Builder;
                bool singleWord = true;

                int index = 0;
                while (index < lineBuilder.Length - markdownIndicator.Length)
                {
                    int openTagIndex = GetNextCharacterIndex(markdownIndicator, index);
                    int closeTagIndex = -1;

                    if (openTagIndex + 1 == lineBuilder.Length) break;

                    if (openTagIndex > -1)
                    {
                        singleWord = lineBuilder[openTagIndex + 1] != '(';
                        closeTagIndex = singleWord 
                            ? GetEndOfWordIndex(openTagIndex + markdownIndicator.Length)
                            : GetNextCharacterIndex(")", openTagIndex + markdownIndicator.Length);
                    }

                    if (closeTagIndex > -1) // If there's a remaining tag pair in the line
                    {
                        if (openTagIndex + markdownIndicator.Length == closeTagIndex)
                        {
                            // Tags have to actually apply to some text. You can write '**' and it doesn't just turn invisible, it shows the asterisks.
                            index = openTagIndex + 1;
                        }
                        else if (IsSpaceBetweenIndexesInvalidForTagging(startIndex: openTagIndex + markdownIndicator.Length, endIndex: closeTagIndex - 1))
                        {
                            index = openTagIndex + 1; ;
                        }
                        else
                        {
                            lineBuilder.ReplaceFirst(markdownIndicator + (singleWord ? "" : "(" ), richTextOpenTag, openTagIndex);
                            lineBuilder.ReplaceFirst((singleWord ? " " : ")" ), richTextCloseTag + (singleWord ? " " : "" ), out int richTextCloseTagStartIndex, openTagIndex + richTextOpenTag.Length - 1);

                            index = richTextCloseTagStartIndex + richTextCloseTag.Length;
                        }
                    }
                    else
                    {
                        break;
                    }
                }


                int GetNextCharacterIndex(string character, int startIndex)
                {
                    return line.UnescapedIndexOf(character, startIndex);
                }

                int GetEndOfWordIndex(int startIndex)
                {
                    return line.UnescapedIndexOf(' ', startIndex);
                }

                bool IsSpaceBetweenIndexesInvalidForTagging(int startIndex, int endIndex)
                {
                    // First, we have to check that the text doesn't begin with the markdown indicator. To solve conflicts, we must always use the last
                    // markdown indicator in a chain. I.e.: in *****example*** we should use the last two asterisks from the first clump as the bold
                    // indicator.
                    if (lineBuilder[startIndex] == markdownIndicator[0])
                        return true;

                    // You can put spaces between stuff to make it not apply. I.e. '* example*' is not italicized.
                    if (lineBuilder[startIndex].IsWhitespace() || lineBuilder[endIndex].IsWhitespace())
                        return true;

                    // Many tags have a defined character that is ignored. So for example you can write "~~~~~" and it's five tildes, not one crossed out tilde.
                    // Furthermore, we dont' tag just escape characters.
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        if (lineBuilder[i] == IgnoreContents)
                            continue;

                        if (lineBuilder[i] == MarkdownLine.EscapeCharacater)
                            continue;

                        return false;
                    }

                    return true;
                }
            }
        }
    }
}
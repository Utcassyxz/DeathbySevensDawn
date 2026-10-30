// 职责：按字体与正文框实际高度拆分字幕；读完当前字幕的所有页后才推进剧情。
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DeathBySeventhsDawn.Interaction
{
    public sealed class DialoguePages
    {
        readonly List<string> pages = new List<string>();
        string source;
        float width, height;
        Font font;
        int fontSize;
        public int Index
        {
            get; private set;
        }
        public int Count => pages.Count;
        public string Current => pages.Count == 0 ? "" : pages[Index];
        public string Counter => Count > 1 ? $"{Index + 1} / {Count}   ·   " : "";
        public void Reset()
        {
            source = null;
            pages.Clear();
            Index = 0;
        }
        public bool Next()
        {
            if (Index + 1 >= pages.Count)
                return false;
            Index++;
            return true;
        }
        public void Set(string value, GUIStyle style, float availableWidth, float availableHeight)
        {
            value = value ?? "";
            if (source == value && width == availableWidth && height == availableHeight && font == style.font && fontSize == style.fontSize)
                return;
            source = value;
            width = Mathf.Max(1, availableWidth);
            height = Mathf.Max(1, availableHeight);
            font = style.font;
            fontSize = style.fontSize;
            Index = 0;
            pages.Clear();
            int start = 0;
            // Text-element boundaries preserve surrogate pairs, combining characters and Chinese punctuation.
            while (start < value.Length)
            {
                string remaining = value.Substring(start);
                if (Fits(remaining, style))
                {
                    pages.Add(remaining);
                    break;
                }
                var boundaries = StringInfo.ParseCombiningCharacters(remaining);
                int low = 1, high = boundaries.Length;
                while (low < high)
                {
                    int mid = (low + high + 1) / 2;
                    int end = mid == boundaries.Length ? remaining.Length : boundaries[mid];
                    if (Fits(remaining.Substring(0, end), style))
                        low = mid;
                    else
                        high = mid - 1;
                }
                int length = low == boundaries.Length ? remaining.Length : boundaries[low];
                // Prefer a sentence/word boundary without wasting most of the page.
                for (int i = length - 1; i >= length / 2; i--)
                    if (char.IsWhiteSpace(remaining[i]) || "。！？.!?；;".IndexOf(remaining[i]) >= 0)
                    {
                        length = i + 1;
                        break;
                    }
                pages.Add(remaining.Substring(0, length));
                start += length;
            }
            if (pages.Count == 0)
                pages.Add("");
        }
        bool Fits(string value, GUIStyle style) => style.CalcHeight(new GUIContent(value), width) <= height - 2;
    }
}

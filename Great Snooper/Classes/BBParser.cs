using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GreatSnooper.Classes
{
    public static class BBParser
    {
        private static Paragraph tempP;
        private static FlowDocument fd;

        public static FlowDocument Parse(string bbcode, FlowDocument thefd = null)
        {
            if (thefd == null)
                fd = new FlowDocument();
            else
                fd = thefd;
            tempP = new Paragraph();
            Parse(tempP, bbcode);
            fd.Blocks.Add(tempP);

            return fd;
        }

        private static void Parse(object e, string bbcode, int idx = 0, int max = int.MaxValue)
        {
            StringBuilder text = new StringBuilder();

            for (int i = idx; i < bbcode.Length && i < max; i++)
            {
                char c = bbcode[i];
                if (c == '[')
                {
                    // Flush the text content if there were
                    if (text.Length != 0)
                    {
                        AddInline(e, new Run(text.ToString()));
                        text.Clear();
                    }


                    // Get the tag and optionally the parameter
                    string tag = string.Empty;
                    string parameter = string.Empty;
                    i++;
                    for (; i < bbcode.Length && i < max; i++)
                    {
                        c = bbcode[i];
                        if (c == ']') // was simple tag
                        {
                            tag = text.ToString();
                            text.Clear();
                            break;
                        }
                        else if (c == '=') // tag has parameter
                        {
                            tag = text.ToString();
                            text.Clear();
                            i++;
                            for (; i < bbcode.Length && i < max; i++)
                            {
                                c = bbcode[i];
                                if (c == ']')
                                {
                                    parameter = text.ToString();
                                    text.Clear();
                                    break;
                                }
                                text.Append(c);
                            }
                            break;
                        }
                        text.Append(c);
                    }

                    if (tag == "br") // new line tag doesn't have closing tag
                    {
                        AddInline(e, new LineBreak());
                    }
                    else if (tag == "space" && e is Paragraph && parameter != string.Empty)
                    {
                        if (tempP.Inlines.Count != 0)
                        {
                            fd.Blocks.Add(tempP);
                            e = tempP = new Paragraph();
                        }
                        tempP.Padding = new Thickness(tempP.Padding.Left, int.Parse(parameter), tempP.Padding.Right, tempP.Padding.Bottom);
                    }
                    else
                    {
                        i++;
                        // search for the closing tag
                        string endtag = "[/" + tag + "]";
                        int m = bbcode.IndexOf(endtag, i);
                        if (m == -1)
                            throw new Exception("Wrong BB code!");

                        switch (tag.ToLower())
                        {
                            case "b":
                                if (e is TextBlock && ((TextBlock)e).Inlines.Count == 0)
                                {
                                    ((TextBlock)e).FontWeight = FontWeights.Bold;
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Hyperlink && ((Hyperlink)e).Inlines.Count == 0)
                                {
                                    ((Hyperlink)e).FontWeight = FontWeights.Bold;
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Paragraph && ((Paragraph)e).Inlines.Count == 0)
                                {
                                    ((Paragraph)e).FontWeight = FontWeights.Bold;
                                    Parse(e, bbcode, i, m);
                                }
                                else
                                {
                                    TextBlock tb = new TextBlock() { FontWeight = FontWeights.Bold };
                                    Parse(tb, bbcode, i, m);
                                    AddInline(e, tb);
                                }
                                break;

                            case "i":
                                if (e is TextBlock && ((TextBlock)e).Inlines.Count == 0)
                                {
                                    ((TextBlock)e).FontStyle = FontStyles.Italic;
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Hyperlink && ((Hyperlink)e).Inlines.Count == 0)
                                {
                                    ((Hyperlink)e).FontStyle = FontStyles.Italic;
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Paragraph && ((Paragraph)e).Inlines.Count == 0)
                                {
                                    ((Paragraph)e).FontStyle = FontStyles.Italic;
                                    Parse(e, bbcode, i, m);
                                }
                                else
                                {
                                    TextBlock tb = new TextBlock() { FontStyle = FontStyles.Italic };
                                    Parse(tb, bbcode, i, m);
                                    AddInline(e, tb);
                                }
                                break;

                            case "u":
                                if (e is TextBlock && ((TextBlock)e).Inlines.Count == 0)
                                {
                                    ((TextBlock)e).TextDecorations.Add(TextDecorations.Underline);
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Hyperlink && ((Hyperlink)e).Inlines.Count == 0)
                                {
                                    ((Hyperlink)e).TextDecorations.Add(TextDecorations.Underline);
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Paragraph && ((Paragraph)e).Inlines.Count == 0)
                                {
                                    ((Paragraph)e).TextDecorations.Add(TextDecorations.Underline);
                                    Parse(e, bbcode, i, m);
                                }
                                else
                                {
                                    TextBlock tb = new TextBlock();
                                    tb.TextDecorations.Add(TextDecorations.Underline);
                                    Parse(tb, bbcode, i, m);
                                    AddInline(e, tb);
                                }
                                break;

                            case "s":
                                if (e is TextBlock && ((TextBlock)e).Inlines.Count == 0)
                                {
                                    ((TextBlock)e).TextDecorations.Add(TextDecorations.Strikethrough);
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Hyperlink && ((Hyperlink)e).Inlines.Count == 0)
                                {
                                    ((Hyperlink)e).TextDecorations.Add(TextDecorations.Strikethrough);
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Paragraph && ((Paragraph)e).Inlines.Count == 0)
                                {
                                    ((Paragraph)e).TextDecorations.Add(TextDecorations.Strikethrough);
                                    Parse(e, bbcode, i, m);
                                }
                                else
                                {
                                    TextBlock tb = new TextBlock();
                                    tb.TextDecorations.Add(TextDecorations.Strikethrough);
                                    Parse(tb, bbcode, i, m);
                                    AddInline(e, tb);
                                }
                                break;

                            case "size":
                                if (e is TextBlock && ((TextBlock)e).Inlines.Count == 0)
                                {
                                    ((TextBlock)e).FontSize = double.Parse(parameter);
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Hyperlink && ((Hyperlink)e).Inlines.Count == 0)
                                {
                                    ((Hyperlink)e).FontSize = double.Parse(parameter);
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Paragraph && ((Paragraph)e).Inlines.Count == 0)
                                {
                                    ((Paragraph)e).FontSize = double.Parse(parameter);
                                    Parse(e, bbcode, i, m);
                                }
                                else
                                {
                                    TextBlock tb = new TextBlock();
                                    tb.FontSize = double.Parse(parameter);
                                    Parse(tb, bbcode, i, m);
                                    AddInline(e, tb);
                                }
                                break;

                            case "color":
                                if (e is TextBlock && ((TextBlock)e).Inlines.Count == 0)
                                {
                                    ((TextBlock)e).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(parameter));
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Hyperlink && ((Hyperlink)e).Inlines.Count == 0)
                                {
                                    ((Hyperlink)e).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(parameter));
                                    Parse(e, bbcode, i, m);
                                }
                                else if (e is Paragraph && ((Paragraph)e).Inlines.Count == 0)
                                {
                                    ((Paragraph)e).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(parameter));
                                    Parse(e, bbcode, i, m);
                                }
                                else
                                {
                                    TextBlock tb = new TextBlock();
                                    ((TextBlock)e).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(parameter));
                                    Parse(tb, bbcode, i, m);
                                    AddInline(e, tb);
                                }
                                break;

                            case "c":
                                if (e is Paragraph)
                                {
                                    if (tempP.Inlines.Count != 0)
                                    {
                                        fd.Blocks.Add(tempP);
                                        e = tempP = new Paragraph();
                                    }
                                    tempP.TextAlignment = TextAlignment.Center;
                                    Parse(e, bbcode, i, m);
                                    fd.Blocks.Add(tempP);
                                }
                                break;

                            case "l":
                                if (e is Paragraph)
                                {
                                    if (tempP.Inlines.Count != 0)
                                    {
                                        fd.Blocks.Add(tempP);
                                        e = tempP = new Paragraph();
                                    }
                                    tempP.TextAlignment = TextAlignment.Left;
                                    if (parameter != string.Empty)
                                    {
                                        tempP.Padding = new Thickness(int.Parse(parameter), tempP.Padding.Top, tempP.Padding.Right, tempP.Padding.Bottom);
                                    }
                                    Parse(e, bbcode, i, m);
                                    fd.Blocks.Add(tempP);
                                }
                                break;

                            case "r":
                                if (e is Paragraph)
                                {
                                    if (tempP.Inlines.Count != 0)
                                    {
                                        fd.Blocks.Add(tempP);
                                        e = tempP = new Paragraph();
                                    }
                                    tempP.TextAlignment = TextAlignment.Right;
                                    if (parameter != string.Empty)
                                    {
                                        tempP.Padding = new Thickness(tempP.Padding.Left, tempP.Padding.Top, int.Parse(parameter), tempP.Padding.Bottom);
                                    }
                                    Parse(e, bbcode, i, m);
                                    fd.Blocks.Add(tempP);
                                }
                                break;


                            case "url":
                                Hyperlink h = new Hyperlink();
                                if (parameter == string.Empty)
                                {
                                    string url = bbcode.Substring(i, m - i);
                                    h.Tag = url;
                                }
                                else
                                    h.Tag = parameter;
                                h.Click += LinkClicked;
                                Parse(h, bbcode, i, m);
                                AddInline(e, h);
                                break;

                            case "img":
                                if (e is Paragraph)
                                {
                                    string src = bbcode.Substring(i, m - i);
                                    Image img = new Image();
                                    img.Source = new BitmapImage(new Uri(src, UriKind.Absolute));
                                    tempP.Inlines.Add(img);
                                }
                                break;
                        }

                        i = m + endtag.Length - 1;
                    }
                }
                else
                {
                    text.Append(c);
                }
            }

            if (text.Length != 0)
            {
                if (e is Run)
                    ((Run)e).Text = text.ToString();
                else
                    AddInline(e, new Run(text.ToString()));
            }
        }

        private static void LinkClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start((string)((Hyperlink)sender).Tag);
            }
            catch (Exception) { }
        }

        private static void AddInline(object e, Inline i)
        {
            if (e is Paragraph)
                ((Paragraph)e).Inlines.Add(i);
            else if (e is Hyperlink)
                ((Hyperlink)e).Inlines.Add(i);
            else if (e is TextBlock)
                ((TextBlock)e).Inlines.Add(i);
        }

        private static void AddInline(object e, TextBlock tb)
        {
            if (e is Paragraph)
                ((Paragraph)e).Inlines.Add(tb);
            else if (e is Hyperlink)
                ((Hyperlink)e).Inlines.Add(tb);
            else if (e is TextBlock)
                ((TextBlock)e).Inlines.Add(tb);
        }
    }
}
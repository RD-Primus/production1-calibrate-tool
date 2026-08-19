using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CMATestVer1
{
    public static class BigMessageBox
    {
        public static DialogResult Show(string text, string caption,
            MessageBoxIcon icon = MessageBoxIcon.Information,
            MessageBoxButtons buttons = MessageBoxButtons.OK,   // ← เพิ่ม parameter นี้
            float fontSize = 14f,
            int lineSpacing = 8,
            int maxWidth = 420)
        {
            Font textFont = new Font("Tahoma", fontSize, FontStyle.Regular);
            Font btnFont = new Font("Tahoma", fontSize - 1f, FontStyle.Regular);

            int maxTextWidth = maxWidth;
            const int iconSize = 40;
            const int padding = 20;
            const int gapIconText = 15;
            const int gapTextButton = 25;
            const int gapBetweenButtons = 12;

            List<string> lines = WrapText(text, textFont, maxTextWidth);

            int lineHeight = TextRenderer.MeasureText("A", textFont).Height;
            int maxLineWidth = 0;
            foreach (var line in lines)
            {
                int w = TextRenderer.MeasureText(line, textFont).Width;
                if (w > maxLineWidth) maxLineWidth = w;
            }

            int textBlockHeight = (lines.Count * lineHeight) + ((lines.Count - 1) * lineSpacing);

            int contentAreaHeight = Math.Max(iconSize, textBlockHeight);
            int iconY = padding + (contentAreaHeight - iconSize) / 2;
            int textY = padding + (contentAreaHeight - textBlockHeight) / 2;

            using (Form form = new Form())
            {
                form.Text = caption;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowIcon = false;
                form.ShowInTaskbar = false;
                form.AutoSize = false;

                PictureBox pic = new PictureBox
                {
                    Image = icon switch
                    {
                        MessageBoxIcon.Error => SystemIcons.Error.ToBitmap(),
                        MessageBoxIcon.Warning => SystemIcons.Warning.ToBitmap(),
                        MessageBoxIcon.Question => SystemIcons.Question.ToBitmap(),
                        _ => SystemIcons.Information.ToBitmap()
                    },
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(iconSize, iconSize),
                    Location = new Point(padding, iconY)
                };

                Panel textPanel = new Panel
                {
                    Location = new Point(padding + iconSize + gapIconText, textY),
                    Size = new Size(maxLineWidth + 10, textBlockHeight + 10),
                    BackColor = Color.Transparent
                };

                textPanel.Paint += (s, e) =>
                {
                    int y = 0;
                    foreach (var line in lines)
                    {
                        TextRenderer.DrawText(e.Graphics, line, textFont,
                            new Point(0, y), Color.Black,
                            TextFormatFlags.Left | TextFormatFlags.NoPadding);
                        y += lineHeight + lineSpacing;
                    }
                };

                int formWidth = padding + iconSize + gapIconText + textPanel.Width + padding;
                if (formWidth < 320) formWidth = 320;

                int contentBottom = Math.Max(pic.Bottom, textPanel.Bottom);

                // ── สร้างปุ่มตามที่เลือก ──
                List<Button> btnList = new List<Button>();

                if (buttons == MessageBoxButtons.YesNo)
                {
                    Button btnYes = MakeButton("Yes", btnFont, DialogResult.Yes);
                    Button btnNo = MakeButton("No", btnFont, DialogResult.No);
                    btnList.Add(btnYes);
                    btnList.Add(btnNo);
                }
                else if (buttons == MessageBoxButtons.OKCancel)
                {
                    Button btnOkC = MakeButton("OK", btnFont, DialogResult.OK);
                    Button btnCancel = MakeButton("Cancel", btnFont, DialogResult.Cancel);
                    btnList.Add(btnOkC);
                    btnList.Add(btnCancel);
                }
                else // default: OK อย่างเดียว
                {
                    Button btnOk = MakeButton("OK", btnFont, DialogResult.OK);
                    btnList.Add(btnOk);
                }

                // ── จัดตำแหน่งปุ่มจากขวาไปซ้าย ──
                int totalBtnWidth = 0;
                foreach (var b in btnList) totalBtnWidth += b.Width;
                totalBtnWidth += gapBetweenButtons * (btnList.Count - 1);

                int startX = formWidth - padding - totalBtnWidth;
                int btnY = contentBottom + gapTextButton;
                int currentX = startX;

                foreach (var b in btnList)
                {
                    b.Location = new Point(currentX, btnY);
                    currentX += b.Width + gapBetweenButtons;
                    form.Controls.Add(b);
                }

                // ปุ่มแรกในลิสต์เป็น default (Enter), ปุ่มสุดท้ายเป็น Cancel (Esc)
                form.AcceptButton = btnList[0];
                form.CancelButton = btnList[^1];

                int formBottomButtonY = btnY + btnList[0].Height;
                form.ClientSize = new Size(formWidth, formBottomButtonY + padding);

                form.Controls.Add(pic);
                form.Controls.Add(textPanel);

                return form.ShowDialog();
            }
        }

        private static Button MakeButton(string text, Font font, DialogResult result)
        {
            Size btnSize = TextRenderer.MeasureText(text, font);
            return new Button
            {
                Text = text,
                Font = font,
                DialogResult = result,
                Size = new Size(Math.Max(btnSize.Width + 40, 90), btnSize.Height + 16)
            };
        }

        private static List<string> WrapText(string text, Font font, int maxWidth)
        {
            var lines = new List<string>();
            var paragraphs = text.Replace("\r\n", "\n").Split('\n');   // ← เพิ่มบรรทัดนี้

            foreach (var paragraph in paragraphs)
            {
                var words = paragraph.Split(' ');
                string currentLine = "";

                foreach (var word in words)
                {
                    string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    var size = TextRenderer.MeasureText(testLine, font);

                    if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }
                lines.Add(currentLine);
            }

            return lines;
        }
    }
}
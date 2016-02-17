using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Image2CSharp
{
    public partial class Image2CSharp : Form
    {
        private List<FileInfo> _files;
        private SortedDictionary<string, string> _convertedImages; 

        public Image2CSharp()
        {
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Filter = "Image Files(*.PNG;*.BMP;*.JPG;*.GIF)|*.PNG;*.BMP;*.JPG;*.GIF|All files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = true,
            };


            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _files = new List<FileInfo>(openFileDialog.FileNames.Length);
                foreach (string file in openFileDialog.FileNames)
                {
                    _files.Add(new FileInfo(file));
                }
            }

            PopulateList();
            ConvertImages();
            CreateCode();
        }

        private void PopulateList()
        {
            foreach (FileInfo file in _files)
            {
                listFiles.Items.Add(file.Name);
            }
        }

        private void ConvertImages()
        {
            _convertedImages = new SortedDictionary<string, string>();
            Parallel.ForEach(_files, f =>
            {
                string base64 = ImageToBase64(Image.FromFile(f.FullName, true));
                string imageName = f.Name.Split('.')[0];
                imageName = imageName[0].ToString().ToUpper()[0] + imageName.Remove(0,1);
                imageName = imageName.Replace("-","");
                if (!_convertedImages.ContainsKey(imageName)) _convertedImages.Add(imageName, base64);
            });
        }

        public string ImageToBase64(Image image)
        {
            string base64String = string.Empty;

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    image.Save(stream, image.RawFormat);
                    byte[] imageBytes = stream.ToArray();

                    base64String = Convert.ToBase64String(imageBytes);
                }
            }
            catch
            {
                // ignored
            }

            return base64String;
        }
        

        private void CreateCode()
        {
            StringBuilder stringBuilder = new StringBuilder("using System;");
            stringBuilder.AppendLine("using System.Drawing;")
                .AppendLine("using System.IO;")
                .AppendLine("using System.Text.RegularExpressions;")
                .AppendLine()
                .AppendLine("namespace *your namespace*")
                .AppendLine("{")
                .AppendLine("class Images".PadLeft(4))
                .AppendLine("{".PadLeft(4))
                .AppendLine("private static Image GetIconFromBase64DataUri(string uri)".PadLeft(8))
                .AppendLine("{".PadLeft(8))
                .AppendLine("string base64Data = Regex.Match(uri, @\"data: image / (?<type>.+?), (?<data>.+)\")" +
                            ".Groups[\"data\"].Value;".PadLeft(12))
                .AppendLine("byte[] binData = Convert.FromBase64String(base64Data);".PadLeft(12))
                .AppendLine()
                .AppendLine("Image image;".PadLeft(12))
                .AppendLine("using (MemoryStream stream = new MemoryStream(binData))".PadLeft(12))
                .AppendLine("{".PadLeft(12))
                .AppendLine("image = new Bitmap(stream);".PadLeft(16))
                .AppendLine("}".PadLeft(12))
                .AppendLine("return image;".PadLeft(12))
                .AppendLine("}".PadLeft(8))
                .AppendLine();

            foreach (string image in _convertedImages.Keys)
            {
                stringBuilder.AppendLine(("public static Image " + image + "()").PadLeft(8))
                    .AppendLine("{".PadLeft(8))
                    .AppendLine("return GetIconFromBase64DataUri(".PadLeft(12))
                    .AppendLine(("\"data:image/png;base64," + _convertedImages[image] + "\"); ").PadLeft(16))
                    .AppendLine("}".PadLeft(8))
                    .AppendLine();
            }

            stringBuilder.AppendLine("}".PadLeft(4)).AppendLine("}");

            textBox1.Text = stringBuilder.ToString();
        }
    }
}

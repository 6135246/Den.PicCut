using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Den.PicCut
{
    public partial class frmMain : Form
    {
        const int SRCCOPY = 0x00CC0020;
        int mMaxSizeX, mMaxSizeY;
        string mFileName, mFileExt;
        bool mIsCompress = false;
        bool mIsStarted = false;
        string mStrDirect = "";
        delegate void UpdateReportCallBack(string pReport);
        public frmMain()
        {
            InitializeComponent();
        }

        private void btnSelectImg_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtImgSource.Text = openFileDialog1.FileName;
                FileInfo oFil = new FileInfo(txtImgSource.Text);
                mFileName = oFil.Name.ToLower().Replace(oFil.Extension.ToLower(),"");
                mFileExt = oFil.Extension;
                Image oImage = Bitmap.FromFile(txtImgSource.Text);
                txtBaseWidth.Text = oImage.Width.ToString();
                txtBaseHeight.Text= oImage.Height.ToString();
                oImage.Dispose();
            }
        }

        private void btnSelectDir_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSaveDir.Text = folderBrowserDialog1.SelectedPath;
            }
        }
      

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtImgSource.Text.Trim()) )
            {
                MessageBox.Show("需要裁剪的文件不能为空");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtSaveDir.Text.Trim()))
            {

                MessageBox.Show("裁剪后的文件路径不能为空！");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtHeight.Text.Trim()))
            {

                MessageBox.Show("文件高度不能为空！");
                return;
            }
            if (!File.Exists(txtImgSource.Text))
            {
                MessageBox.Show("文件不存在！");

                return;
            }
            richTextBox1.Text = "";

            if (mIsStarted)
            {
                mIsStarted = false;
                btnSelectDir.Enabled = true;
                btnSelectImg.Enabled = true;
                btnStart.Text = "开始裁剪";
            }
            else
            {
               
                mIsStarted = true;                
                btnStart.Text = "停止裁剪";
                btnSelectDir.Enabled = false;
                btnSelectImg.Enabled = false;
                Thread oThread = new Thread(new ThreadStart(StartCut));
                oThread.Start();
            }
        }
       
        private void StartCut()
        {
            mStrDirect = txtSaveDir.Text.Trim() + "\\" + mFileName + "\\";
            if (!Directory.Exists(mStrDirect))
            {
                Directory.CreateDirectory(mStrDirect);
            }
            int oHeight = Convert.ToInt32(txtHeight.Text);
            int oIndex = 0;
            int oCutHeight = 0;
            Image oImage = Bitmap.FromFile(txtImgSource.Text);
            mMaxSizeX = oImage.Width;
            mMaxSizeY = oImage.Height;
            UpdateReport("开始裁剪！");
            while (mIsStarted)
            {
                if (mMaxSizeY <= oHeight)
                {
                    oHeight = mMaxSizeY;
                    mIsStarted = false;
                    UpdateReport("全部生成完成！");
                }
                
                Cut(oIndex, 0, oCutHeight, oImage.Width, oHeight, oImage.Width, oImage.Height, oImage);
                mMaxSizeY = mMaxSizeY - oHeight;
                oCutHeight += oHeight;
                oIndex += 1;
            }
            oImage.Dispose();
        }
       
        public void Cut(int pIndex, int pStartX, int pStartY, int pWidth, int pHeight, int pDefaultWidth, int pDefaultHeight, Image pBaseImage)
        {
            int i_x = pStartX,               // 裁切开始横坐标
                        i_y = pStartY,               // 裁切开始纵坐标
                        i_w = pWidth,               // 裁切宽度
                        i_h = pHeight,               // 裁切高度
                        i_boundx = pDefaultWidth,     // 页面图片初始宽度
                        i_boundy = pDefaultHeight,     // 页面图片初始高度
                        s_w = pBaseImage.Width,                                           // 图片原始宽度
                        s_h = pBaseImage.Height;                                          // 图片原始高度
            double scale = 1,                                                   // 实际比例
                s_scale = 1;                                                    // 伸缩比例

            // 页面显示图片时，最大宽度和高度为400，所以用400进行比较
            if (s_w > 400 || s_h > 400)
            {
                // 需要计算伸缩比例
                if (s_w >= s_h)
                {
                    // 图片宽度大于高度，按宽度伸缩比例计算
                    s_scale = double.Parse(i_boundx.ToString()) / 400.00;
                    s_scale = double.Parse(s_scale.ToString("#0.00"));
                    // 计算实际比例
                    scale = double.Parse(s_w.ToString()) / 400.00;
                    scale = double.Parse(scale.ToString("#0.00"));
                }
                else
                {
                    // 图片高度大于宽度，按高度伸缩比例计算
                    s_scale = double.Parse(i_boundy.ToString()) / 400.00;
                    s_scale = double.Parse(s_scale.ToString("#0.00"));
                    // 计算实际比例
                    scale = double.Parse(s_h.ToString()) / 400.00;
                    scale = double.Parse(scale.ToString("#0.00"));
                }
            }

            // 计算实际的裁切开始位置和宽高
            int t_x = Convert.ToInt32(i_x / s_scale * scale),
                t_y = Convert.ToInt32(i_y / s_scale * scale),
                t_w = Convert.ToInt32(i_w / s_scale * scale),
                t_h = Convert.ToInt32(i_h / s_scale * scale);
            Bitmap destBitmap = null;
            // 目标图容器
            Rectangle destRect = new Rectangle();
            // 原图截取区域
            Rectangle srcRect = new Rectangle();
            System.Drawing.Imaging.ImageFormat iformat;
            // 目标图
            destBitmap = new Bitmap(t_w, t_h);
            // 目标图容器
            destRect = new Rectangle(0, 0, t_w, t_h);
            // 原图截取区域
            srcRect = new Rectangle(t_x, t_y, t_w, t_h);
            //从指定的Image对象创建新Graphics对象
            Graphics graphics = Graphics.FromImage(destBitmap);
            switch (mFileExt.ToUpper())
            {
                case ".GIF":
                    iformat = System.Drawing.Imaging.ImageFormat.Gif;
                    graphics.Clear(Color.Transparent);
                    break;
                case ".JPG":
                case ".JPEG":
                    iformat = System.Drawing.Imaging.ImageFormat.Jpeg;
                    graphics.Clear(Color.White);
                    break;
                case ".PNG":
                    iformat = System.Drawing.Imaging.ImageFormat.Png;
                    graphics.Clear(Color.Transparent);
                    break;
                case ".BMP":
                    iformat = System.Drawing.Imaging.ImageFormat.Bmp;
                    graphics.Clear(Color.White);
                    break;
                default:
                    iformat = System.Drawing.Imaging.ImageFormat.Png;
                    graphics.Clear(Color.Transparent);
                    break;
            }
            //设置质量
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            string imageSavePath = mStrDirect + pIndex.ToString() + mFileExt;
            if (mIsCompress)
            {
                imageSavePath = txtSaveDir.Text+"\\"+mFileName+mFileExt;
            }
            //在指定位置并且按指定大小绘制原图片对象
            graphics.DrawImage(pBaseImage, destRect, srcRect, GraphicsUnit.Pixel);
            EncoderParameters ep = new EncoderParameters(2);
            ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 70L);
            ep.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 48 + 24L);
            ImageCodecInfo ici = this.getImageCoderInfo(mFileExt);
            destBitmap.Save(imageSavePath, ici, ep);
            UpdateReport(imageSavePath);
        }
        /// <summary>
        /// 获取图片编码类型信息
        /// </summary>
        /// <param name="coderType">编码类型</param>
        /// <returns>ImageCodecInfo</returns>
        private ImageCodecInfo getImageCoderInfo(string pExt)
        {
            ImageCodecInfo[] iciS = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo retIci = null;
            foreach (ImageCodecInfo ici in iciS)
            {
                if (ici.FilenameExtension.Contains(pExt.ToUpper()))
                {
                    retIci = ici;

                }
            }
            return retIci;
        }
        /// <summary>
        /// 状态报告
        /// </summary>
        /// <param name="report"></param>
        private void UpdateReport(string pReport)
        {
            if (this.InvokeRequired)
            {
                UpdateReportCallBack cb = new UpdateReportCallBack(UpdateReport);
                try
                {
                    this.Invoke(cb, pReport);
                }
                catch
                {

                }
            }
            else
            {
                if (pReport.Equals("全部生成完成！"))
                {
                    btnSelectDir.Enabled = true;
                    btnSelectImg.Enabled = true;
                    btnStart.Text = "开始裁剪";
                }
                if (string.IsNullOrWhiteSpace(richTextBox1.Text.Trim()))
                {
                    richTextBox1.Text = pReport;
                }
                else
                {
                    richTextBox1.Text = richTextBox1.Text + "\r\n" + pReport;
                }
            }
        }

        private void StartCompress()
        {
            mStrDirect = txtSaveDir.Text.Trim() + "\\" + mFileName + "\\";
            if (!Directory.Exists(mStrDirect))
            {
                Directory.CreateDirectory(mStrDirect);
            }
           
            int oIndex = 0;
            int oCutHeight = 0;
            Image oImage = Bitmap.FromFile(txtImgSource.Text);
            mMaxSizeX = oImage.Width;
            mMaxSizeY = oImage.Height;
            int oHeight = oImage.Height;
            UpdateReport("开始压缩！");
            while (mIsStarted)
            {
                if (mMaxSizeY <= oHeight)
                {
                    oHeight = mMaxSizeY;
                    mIsStarted = false;
                    UpdateReport("全部生成完成！");
                }

                Cut(oIndex, 0, oCutHeight, oImage.Width, oHeight, oImage.Width, oImage.Height, oImage);
                mMaxSizeY = mMaxSizeY - oHeight;
                oCutHeight += oHeight;
                oIndex += 1;
            }
            oImage.Dispose();
        }
       
        private void btnCompress_Click(object sender, EventArgs e)
        {
            mIsCompress = true;
            mIsStarted = true;
            StartCompress();
            mIsCompress = false;
        }
    }
}

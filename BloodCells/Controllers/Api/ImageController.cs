using System;
using System.Drawing;
using System.IO;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using BloodCells.ViewModels;
using Emgu.CV;
using Emgu.CV.Structure;

namespace BloodCells.Controllers.Api
{
    public class ImageController : ApiController
    {
        private readonly string _input = HttpContext.Current.Server.MapPath("~/Content/Images/Input");
        private readonly string _output = HttpContext.Current.Server.MapPath("~/Content/Images/Output");

        private CascadeClassifier _classifier =
            new CascadeClassifier(HttpContext.Current.Server.MapPath("~/App_Data/bloodcellhaar.xml"));

        [ResponseType(typeof(ImageView))]
        public IHttpActionResult PostImage(ImageView iv)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var data = Convert.FromBase64String(iv.Base64);
                var ms = new MemoryStream(data);
                var bmp = new Bitmap(ms);

                Directory.CreateDirectory(_input);
                var inputName = Guid.NewGuid();
                bmp.Save(Path.Combine(_input, $"{inputName}.jpg"));

                var image = new Image<Bgr, byte>(bmp);
                var gray = image.Convert<Gray, byte>();
                var hsv = image.Convert<Hsv, byte>();
                var mask = FilterBigCells(hsv);
                var edit = image.Clone();

                var cells = _classifier.DetectMultiScale(gray);

                foreach (var cell in cells)
                {
                    //var detected = false;

                    for (int i = cell.X; i < cell.Width + cell.X; i++)
                    {
                        for (int j = cell.Y; j < cell.Height + cell.Y; j++)
                        {
                            if (mask[j, i].Intensity == 255)
                            {
                                edit[j, i] = new Bgr(70, 115, 33);
                                //detected = true;
                            }
                        }
                    }

                    //if (detected)
                    //{
                    //    edit.Draw(cell, new Bgr(0, 0, 0));
                    //}
                }

                Directory.CreateDirectory(_output);
                var outputName = Guid.NewGuid();
                edit.Save(Path.Combine(_output, $"{outputName}.jpg"));

                var ir = new ImageResponse
                {
                    Original = Url.Content($"~/Content/Images/Input/{inputName}.jpg"),
                    Edit = Url.Content($"~/Content/Images/Output/{outputName}.jpg")
                };

                return Ok(ir);
            }
            catch (Exception exception)
            {
                return InternalServerError(exception);
            }
        }

        private Image<Gray, byte> FilterBigCells(Image<Hsv, byte> image)
        {
            //var lower = new Hsv(90, 80, 160);
            //var higher = new Hsv(150, 140, 255);

            var lower = new Image<Bgr, byte>(HttpContext.Current.Server.MapPath("~/App_Data/low.jpg"))
                .Convert<Hsv, byte>()[0, 0];
            var higher = new Image<Bgr, byte>(HttpContext.Current.Server.MapPath("~/App_Data/high.jpg"))
                .Convert<Hsv, byte>()[0, 0];

            return image.InRange(new Hsv(lower.Hue - 10, 100, 100), new Hsv(higher.Hue + 10, 255, 255));
        }
    }
}
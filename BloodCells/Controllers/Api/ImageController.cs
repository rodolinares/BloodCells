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
                var hsv = image.Convert<Hsv, byte>();
                var mask = FilterBigCells(hsv);
                var edit = image.Clone();

                for (int i = 0; i < mask.Rows; i++)
                {
                    for (int j = 0; j < mask.Cols; j++)
                    {
                        if (mask[i, j].Intensity == 255)
                        {
                            edit[i, j] = new Bgr(50, 50, 50);
                        }
                    }
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
            catch (Exception ex)
            {
                return InternalServerError();
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
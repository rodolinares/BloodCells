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
                bmp.Save(Path.Combine(_input, "test.bmp"));

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
                edit.Save(Path.Combine(_output, "test.bmp"));

                var ir = new ImageResponse
                {
                    Url = ""
                };

                return Ok(ir);
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        private Image<Gray, byte> FilterBigCells(Image<Hsv, byte> image)
        {
            var lowerBlue = new Hsv(90, 80, 160);
            var upperBlue = new Hsv(150, 140, 255);
            return image.InRange(lowerBlue, upperBlue);
        }
    }
}
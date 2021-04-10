using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SkiaSharp;

namespace OgImgGen
{
    public class OgImgGenMiddleware
    {
        private readonly RequestDelegate _next;
        protected readonly string defaultImage;
        protected readonly string font;

        public OgImgGenMiddleware(RequestDelegate next,string defaultImage,string font)
        {
            _next = next;
            this.defaultImage = defaultImage;
            this.font = font;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var width = 1200;
            var height = 630;
            var textStart = 480;
            var imageHeight = 300;
            var dark = "dark";
            var light = "light";
            var white = "#ffffff";
            var black = "#000000";
            // Setup frontend caching
            httpContext.Response.ContentType = "image/png";
            httpContext.Response.Headers["Cache-Control"] = "public,max-age=31536000";
            httpContext.Response.Headers["Vary"] =
                new string[] { "Accept-Encoding" };

            // Read variables from queryString
            var topText = httpContext.Request.Query["topText"].ToString();
            var bottomText = httpContext.Request.Query["bottomText"].ToString();
            var theme = httpContext.Request.Query["theme"].ToString().ToLower();
            theme = theme == dark || theme == light ? theme : dark;
            var imagePath = httpContext.Request.Query["image"].ToString();
            imagePath = string.IsNullOrWhiteSpace(imagePath) ? defaultImage : imagePath;
            var fontSize = 48;
            int lineHeight = (int)Math.Round(fontSize * 1.5);
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), imagePath);

            // load logo
            var imageFile = File.ReadAllBytes(logoPath);
            var image = SKBitmap.Decode(imageFile);

            // calculate resize ratio
            var ratio = (double)image.Info.Height / (double)imageHeight;
            ratio = ratio < 1 ? 1 : ratio;

            // resize image
            SKImageInfo resizeInfo = new SKImageInfo((int)(image.Info.Width / ratio), (int)(image.Info.Height / ratio), image.Info.ColorType, image.Info.AlphaType, image.Info.ColorSpace);
            image = image.Resize(resizeInfo, SKBitmapResizeMethod.Box);

            // initialize canvas
            var surface = SKSurface.Create(new SKImageInfo(width, height));
            var bitmapCanvas = surface.Canvas;

            // Draw on canvas
            using (SKPaint textPaint = new SKPaint { TextSize = fontSize,
                Color = SKColor.Parse(theme == dark ? white : black),
                Typeface = SKTypeface.FromFamilyName(Path.Combine(Directory.GetCurrentDirectory(), font)),
                TextAlign = SKTextAlign.Center,
                IsAntialias = true
            })
            {
                SKRect bounds = new SKRect(0,0,width,height);
                bitmapCanvas.Clear(SKColor.Parse(theme == dark ? black : white));

                var RectPaint = new SKPaint { Color = SKColor.Parse(theme == dark ? white : black), Style = SKPaintStyle.Fill };
                for(float i = 0; i <= width; i += 30)
                {
                    for(float k = 0; k<= height; k+= 30)
                    {
                        if (k != height)
                        {
                            if(i!=width)
                            {
                                bitmapCanvas.DrawRect(i, k, 1, 1, RectPaint);
                            } else
                            {
                                bitmapCanvas.DrawRect(i-1, k, 1, 1, RectPaint);
                            }
                        }
                        else
                        {
                            if (i != width)
                            {
                                bitmapCanvas.DrawRect(i, k-1, 1, 1, RectPaint);
                            }
                            else
                            {
                                bitmapCanvas.DrawRect(i - 1, k-1, 1, 1, RectPaint);
                            }
                        }
                    }
                    }
                

                
                bitmapCanvas.DrawBitmap(image, (width - image.Info.Width)/2, 75);

                if (!string.IsNullOrWhiteSpace(topText))
                {
                    // TODO: Check generated text width overflow
                    // var width = textPaint.MeasureText(topText, ref bounds);
                    bitmapCanvas.DrawText(topText, (width / 2), textStart, textPaint);
                }

                if (!string.IsNullOrWhiteSpace(topText))
                {
                    // TODO: Check generated text width overflow
                    // var width = textPaint.MeasureText(bottomText, ref bounds);
                    bitmapCanvas.DrawText(bottomText, (width / 2), textStart + lineHeight, textPaint);
                }      
                
                bitmapCanvas.Save();

                var skdata = surface.Snapshot().Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                await skdata.AsStream().CopyToAsync(httpContext.Response.Body);
            }
        }
    }

    public static class OgImgGenMiddlewareExtension
    {
        public static IApplicationBuilder UseOgImgGen(
            this IApplicationBuilder builder,string url, string defaultImage, string font)
        {
            return builder.Map(url, app => app.UseMiddleware<OgImgGenMiddleware>(defaultImage,font));
        }
    }
}

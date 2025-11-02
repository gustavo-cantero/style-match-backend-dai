using SkiaSharp;

namespace StyleMatch;

public static class ImageResizer
{
    /// <summary>
    /// Lee una imagen desde un Stream, corrige la orientación EXIF, la ajusta a maxSize
    /// (manteniendo aspecto y sin hacer upscale) y la guarda en disco como JPEG.
    /// </summary>
    /// <param name="input">Imagen de entrada (Stream)</param>
    /// <param name="maxSize">Tamaño máximo (ancho o alto) de la imagen resultante</param>
    /// <param name="outputPath">Ruta de salida (archivo JPEG)</param>
    /// <param name="quality">Calidad de compresión (1-100)</param>
    public static void ResizeToJpeg(Stream input, int maxSize, string outputPath, int quality = 85)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Leemos todos los bytes para poder crear SKData y reusar el buffer sin problemas de posición del stream
        input.Position = 0;
        using var data = SKData.Create(input);
        using var codec = SKCodec.Create(data) ?? throw new InvalidDataException("No se pudo leer la imagen");

        var origin = codec.EncodedOrigin;                // Orientación EXIF (SKEncodedOrigin)
        var info = codec.Info;                    // Dimensiones crudas (sin orientación)
        int srcW = info.Width;
        int srcH = info.Height;

        // Dimensiones "orientadas" (si hay rotación 90/270 se invierten)
        var (oriW, oriH) = IsQuarterTurn(origin) ? (srcH, srcW) : (srcW, srcH);

        // Escala manteniendo aspecto y evitando upscale
        float scale = Math.Min((float)maxSize / oriW, (float)maxSize / oriH);
        scale = Math.Min(scale, 1f);
        int dstW = Math.Max(1, (int)Math.Round(oriW * scale));
        int dstH = Math.Max(1, (int)Math.Round(oriH * scale));

        // Decodifico el bitmap original (sin aplicar orientación aún)
        using var original = SKBitmap.Decode(data) ?? throw new InvalidDataException("No se pudo leer la imagen");

        // Surface destino final con tamaño ya escalado
        var dstInfo = new SKImageInfo(dstW, dstH, original.ColorType, original.AlphaType, original.ColorSpace);
        using var surface = SKSurface.Create(dstInfo);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // Pintura de alta calidad
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High, IsDither = true };

        // Escalamos el lienzo en función del tamaño orientado → tamaño destino
        float s = scale; // mismo factor para ambos ejes
        canvas.Scale(s, s);

        // Aplicamos la transformación de orientación en el sistema de coordenadas original (srcW x srcH)
        ApplyExifOrigin(canvas, origin, srcW, srcH);

        // Dibujamos el bitmap crudo; las transformaciones previas corrigen rotación/espejado
        canvas.DrawBitmap(original, 0, 0, paint);
        canvas.Flush();

        // Guardar como JPEG
        using var snapshot = surface.Snapshot();
        using var dataJpeg = snapshot.Encode(SKEncodedImageFormat.Jpeg, quality);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var fsOut = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        dataJpeg.SaveTo(fsOut);
    }

    private static bool IsQuarterTurn(SKEncodedOrigin origin) =>
        origin == SKEncodedOrigin.LeftTop || // 5
        origin == SKEncodedOrigin.RightTop || // 6
        origin == SKEncodedOrigin.RightBottom || // 7
        origin == SKEncodedOrigin.LeftBottom;    // 8

    /// <summary>
    /// Aplica al canvas la transformación correspondiente al EXIF (rotaciones/espejados).
    /// Referencia: valores de SKEncodedOrigin (1..8).
    /// </summary>
    private static void ApplyExifOrigin(SKCanvas canvas, SKEncodedOrigin origin, int width, int height)
    {
        switch (origin)
        {
            case SKEncodedOrigin.TopLeft:
                // 1) Identidad
                break;

            case SKEncodedOrigin.TopRight:
                // 2) Espejo horizontal
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;

            case SKEncodedOrigin.BottomRight:
                // 3) Rotar 180
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;

            case SKEncodedOrigin.BottomLeft:
                // 4) Espejo vertical
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;

            case SKEncodedOrigin.LeftTop:
                // 5) Transpose (rotar 90 y espejar vertical)
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;

            case SKEncodedOrigin.RightTop:
                // 6) Rotar 90 (CW)
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;

            case SKEncodedOrigin.RightBottom:
                // 7) Transverse (rotar 270 y espejar horizontal)
                canvas.Translate(width, height);
                canvas.RotateDegrees(270);
                break;

            case SKEncodedOrigin.LeftBottom:
                // 8) Rotar 270 (CW)
                canvas.Translate(0, height);
                canvas.RotateDegrees(270);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Crea una miniatura cuadrada centrada respetando EXIF
    /// </summary>
    /// <param name="allowUpscale">Permitir upscale si la imagen es más pequeña que el tamaño objetivo</param>
    /// <param name="input">Imagen de entrada (Stream)</param>
    /// <param name="outputPath">Ruta de salida para la miniatura (JPEG)</param>
    /// <param name="quality">Calidad JPEG (1..100)</param>
    /// <param name="size">Tamaño de la miniatura (px)</param>
    public static void CreateThumb(Stream input, int size, string outputPath, int quality = 85, bool allowUpscale = true)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        if (quality < 1 || quality > 100) throw new ArgumentOutOfRangeException(nameof(quality));

        // Cargamos bytes en memoria para poder usar SKCodec (EXIF) y decodificar varias veces sin reposicionar el stream
        input.Position = 0;
        using var data = SKData.Create(input);
        using var codec = SKCodec.Create(data);
        if (codec == null) throw new InvalidDataException("No se pudo decodificar la imagen.");

        var origin = codec.EncodedOrigin;
        var info = codec.Info;           // dimensiones crudas (sin orientación)
        int srcW = info.Width;
        int srcH = info.Height;

        // Dimensiones "orientadas" (si hay rotación 90/270, se invierten)
        var (oriW, oriH) = IsQuarterTurn(origin) ? (srcH, srcW) : (srcW, srcH);

        // Decodificamos bitmap crudo (aún sin aplicar orientación)
        using var original = SKBitmap.Decode(data);
        if (original == null) throw new InvalidDataException("No se pudo decodificar la imagen.");

        // 1) Generamos una superficie "orientada" (sin escalar) para trabajar en coordenadas ya corregidas
        var orientedInfo = new SKImageInfo(oriW, oriH, original.ColorType, original.AlphaType, original.ColorSpace);
        using var orientedSurface = SKSurface.Create(orientedInfo);
        var ocanvas = orientedSurface.Canvas;
        ocanvas.Clear(SKColors.Transparent);

        // Dibujamos el bitmap aplicando la orientación EXIF
        ApplyExifOrigin(ocanvas, origin, srcW, srcH);
        using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High, IsDither = true })
        {
            ocanvas.DrawBitmap(original, 0, 0, paint);
        }
        ocanvas.Flush();

        using var orientedImage = orientedSurface.Snapshot();

        // 2) Calculamos el recorte cuadrado centrado en el espacio orientado
        int side = Math.Min(oriW, oriH);
        int cropX = (oriW - side) / 2;
        int cropY = (oriH - side) / 2;
        var srcRect = new SKRectI(cropX, cropY, cropX + side, cropY + side);

        // 3) Definimos el tamaño final (evitar upscale si se pide)
        int target = size;
        if (!allowUpscale && side < size)
            target = side; // miniatura puede ser más chica para no ampliar

        // 4) Render de la miniatura (escala del recorte cuadrado al destino cuadrado)
        var thumbInfo = new SKImageInfo(target, target, orientedInfo.ColorType, orientedInfo.AlphaType, orientedInfo.ColorSpace);
        using var thumbSurface = SKSurface.Create(thumbInfo);
        var tcanvas = thumbSurface.Canvas;
        tcanvas.Clear(SKColors.Transparent);

        using (var paint2 = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High, IsDither = true })
        {
            tcanvas.DrawImage(
                orientedImage,
                srcRect,                        // recorte central cuadrado
                new SKRect(0, 0, target, target), // destino cuadrado
                paint2
            );
        }
        tcanvas.Flush();

        // 5) Guardar como JPEG
        using var thumbSnapshot = thumbSurface.Snapshot();
        using var jpg = thumbSnapshot.Encode(SKEncodedImageFormat.Jpeg, quality);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var fsOut = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        jpg.SaveTo(fsOut);
    }

}
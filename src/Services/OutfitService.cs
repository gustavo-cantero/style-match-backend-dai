using SkiaSharp;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StyleMatch.Services;

public sealed class GenerateOutfitOptions
{
    public string Size { get; set; } = "1024x1024";  // "1024x1024", "768x768", etc.
    public int TileSize { get; set; } = 1024;
    public int Padding { get; set; } = 4;
    public string Quality { get; set; } = "high";    // "auto" | "high"
}

public static class OutfitService
{
    /// <summary>
    /// Genera 
    /// </summary>
    /// <param name="imagePaths"></param>
    /// <param name="openAiApiKey"></param>
    /// <param name="opts"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<byte[]> GenerateOutfitAsync(
        IEnumerable<string> imagePaths,
        string? openAiApiKey = null,
        GenerateOutfitOptions? opts = null)
    {
        if (imagePaths == null || !imagePaths.Any())
            throw new ArgumentException("Se requiere al menos una imagen.", nameof(imagePaths));

        // Validar que existan (y filtrar nulos)
        var paths = imagePaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        foreach (var p in paths)
            if (!File.Exists(p))
                throw new FileNotFoundException($"No se encontró el archivo: {p}", p);

        opts ??= new GenerateOutfitOptions();

        // 1) Componer atlas desde archivos
        using Stream atlas = ComposeAtlas(paths, opts.TileSize, opts.Padding);

        // 2) Prompt
        int count = paths.Count;
        string prompt = string.Join(" ", new[]
        {
            "Usar la imagen base (atlas de prendas) como única referencia.",
            "Generar una foto fotorrealista de un maniquí de plástico blanco mate de cuerpo completo, sin rasgos faciales, fondo neutro e iluminación de estudio.",
            $"Debe aparecer exactamente {count} prenda{(count == 1 ? "" : "s")} tomadas del atlas, sin agregar, completar ni inventar otras prendas, accesorios, logos ni textos.",
            "Cada prenda debe conservar exactamente su color, material, textura y patrón, sin copiar ni trasladar logos/estampados de una prenda a otra.",
            "Queda estrictamente prohibido duplicar, reflejar, proyectar o fusionar logos, parches, escudos, textos, gráficos o patrones de una prenda sobre otra.",
            "Si hay varias prendas, aplicar superposición opaca y realista: la prenda externa cubre la interna donde corresponda; nada de transparencias.",
            "Cualquier detalle de la prenda interna que quede cubierto por la externa no debe ser visible.",
            "Si una zona del cuerpo no tiene prenda en el atlas, mostrar el material blanco del maniquí en esa zona.",
            "No trasladar ni inventar logos, parches, escudos, tipografías, símbolos ni gráficos sobre prendas que no los tengan en el atlas.",
            "No mezclar texturas entre capas. Sin estampados fantasma, sin bordes difuminados, sin transparencias entre prendas.",
            "En caso de duda sobre la existencia o visibilidad de una prenda o detalle, NO lo generes.",
            "Si un elemento de la imagen esta marcado como no visible o queda por debajo de otra prenda, NO MOSTRARLO"
        });

        // 3) POST /v1/images/edits (gpt-image-1)
        using HttpClient http = new();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiApiKey);

        using var form = new MultipartFormDataContent
        {
            { new StringContent("gpt-image-1", Encoding.UTF8), "model" },
            { new StringContent(prompt, Encoding.UTF8), "prompt" },
            { new StringContent(opts.Size, Encoding.UTF8), "size" },
            { new StringContent(opts.Quality, Encoding.UTF8), "quality" },
            { new StringContent("1", Encoding.UTF8), "n" },
            //{ new StringContent("b64_json", Encoding.UTF8), "response_format" }
        };

        StreamContent imageContent = new(atlas);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(imageContent, "image", "atlas.png");

        using var resp = await http.PostAsync("https://api.openai.com/v1/images/edits", form);
        string payload = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI Images Edit falló ({(int)resp.StatusCode}): {payload}");

        var parsed = JsonSerializer.Deserialize<ImageEditResponse>(payload);
        string? b64 = parsed?.Data?.FirstOrDefault()?.B64Json;
        if (string.IsNullOrWhiteSpace(b64))
            throw new InvalidOperationException("No se generó la imagen.");

        //Devuelvo la imagen en binario
        return Convert.FromBase64String(b64);
    }

    /// <summary>
    /// Devuelve un PNG con el atlas de las imágenes
    /// </summary>
    /// <param name="paths">Rutas de las imágenes a componer</param>
    /// <param name="tileSize">Tamaño de cada celda en el atlas</param>
    /// <param name="padding">Espaciado entre celdas</param>
    /// <returns>Stream con el PNG del atlas generado</returns>
    public static Stream ComposeAtlas(IList<string> paths, int tileSize, int padding)
    {
        if (paths == null || paths.Count == 0)
            throw new ArgumentException("Se requiere al menos una imagen.", nameof(paths));

        int n = paths.Count;
        int cols = (int)Math.Ceiling(Math.Sqrt(n));
        int rows = (int)Math.Ceiling(n / (double)cols);

        int cell = tileSize;
        int width = cols * cell + (cols + 1) * padding;
        int height = rows * cell + (rows + 1) * padding;

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        for (int i = 0; i < n; i++)
        {
            string file = paths[i];
            using var bmp = SKBitmap.Decode(file); // SkiaSharp decodifica PNG/JPG/WebP, etc.
            if (bmp == null) continue;

            int col = i % cols;
            int row = i / cols;

            int x0 = padding + col * (cell + padding);
            int y0 = padding + row * (cell + padding);

            float scale = Math.Min(cell / (float)bmp.Width, cell / (float)bmp.Height);
            int drawW = (int)Math.Round(bmp.Width * scale);
            int drawH = (int)Math.Round(bmp.Height * scale);

            int dx = x0 + (cell - drawW) / 2;
            int dy = y0 + (cell - drawH) / 2;

            var dest = new SKRect(dx, dy, dx + drawW, dy + drawH);
            canvas.DrawBitmap(bmp, dest);
        }

        canvas.Flush();
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        //Copio el stream, porque el "using" lo cierra al salir del método
        using var src = data.AsStream();
        MemoryStream mem = new();
        src.CopyTo(mem);
        mem.Position = 0;
        return mem;
    }

    #region Datos de deserialización de OpenAI

    private sealed class ImageEditResponse
    {
        [JsonPropertyName("data")]
        public List<ImageEditDatum>? Data { get; set; }
    }

    private sealed class ImageEditDatum
    {
        [JsonPropertyName("b64_json")]
        public string? B64Json { get; set; }
    }

    #endregion
}

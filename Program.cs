using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var imageTitles = new Dictionary<string, string>();
var imageCounter = 0;

app.MapGet("/", async (HttpContext context) =>
{
    var indexHtml = await File.ReadAllTextAsync("Index.html");
    await context.Response.WriteAsync(indexHtml);
});

app.MapPost("/upload", async (HttpContext context) =>
{
    var file = context.Request.Form.Files.GetFile("image");
    var title = context.Request.Form["title"];
    if (file != null && file.Length > 0)
    {
        // Check the file extension
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(fileExtension.ToLower()))
        {
            await context.Response.WriteAsync("Invalid file extension. Only JPEG, PNG, and GIF files are allowed.");
            return;
        }

        // Generate an incremental number
        imageCounter++;
        var uniqueFileName = imageCounter.ToString() + fileExtension;

        // Save the file to a wwwroot/uploads directory
        var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDirectory);
        var filePath = Path.Combine(uploadsDirectory, uniqueFileName);
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        // Store the title in the dictionary
        imageTitles.Add(uniqueFileName, title);

        // Save the dictionary to a JSON file
        var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imageTitles.json");
        var json = JsonSerializer.Serialize(imageTitles);
        await File.WriteAllTextAsync(jsonFilePath, json);

        // Redirect to the separate page that displays the uploaded image
        context.Response.Redirect($"/image/{uniqueFileName}");
    }
    else
    {
        // Handle the case where no file was selected for upload
        context.Response.Redirect("/");
    }
});

app.MapGet("/image/{fileName}", async (HttpContext context) =>
{
    var fileName = context.Request.RouteValues["fileName"].ToString();
    var imagePath = $"{context.Request.Scheme}://{context.Request.Host}/uploads/{fileName}";

    // Check if the image file exists
    if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName)))
    {
        // Retrieve the title from the dictionary
        var title = imageTitles.ContainsKey(fileName) ? imageTitles[fileName] : "";

        var imageHtml = await File.ReadAllTextAsync("Image.html");
        imageHtml = imageHtml.Replace("{{title}}", title);
        imageHtml = imageHtml.Replace("{{imagePath}}", imagePath);

        await context.Response.WriteAsync(imageHtml);
    }
    else
    {
        // Handle the case where the image file does not exist
        var error = await File.ReadAllTextAsync("error.html");
        await context.Response.WriteAsync(error);
    }
});
app.UseStaticFiles();
app.Run();

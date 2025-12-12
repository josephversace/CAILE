using System.Runtime.CompilerServices;
using System.Text;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using Microsoft.ML.OnnxRuntimeGenAI;
using NPOI.SS.Formula.Functions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Dapper.SqlMapper;

namespace IIM.Api.Models;

public class MultimodalOnnxAgent : IDisposable
{
	private readonly Model _model;
	private readonly MultiModalProcessor _processor;
	private readonly Tokenizer _tokenizer;

	public MultimodalOnnxAgent(string modelFolder)
	{
		_model = new Model(modelFolder);
		_processor = new MultiModalProcessor(_model);
		_tokenizer = new Tokenizer(_model);
	}

	/// <summary>
	/// Runs multimodal inference using a prompt + raw image bytes.
	/// </summary>
	public async Task<string> GenerateAsync(string prompt, byte[] imageBytes)
	{
		var ortImages = Images.Load(imageBytes);

		// Build the Phi-3.5 Vision ONNX-required prompt format
//		string formattedPrompt =
//			@$"<|user|>
//<|image_1|>
//[SYSTEM INSTRUCTIONS]
//You are CAILE - Vision, an investigative multimodal analysis model.
//Your purpose is to help digital forensics analysts examine images, documents, and screenshots.

//Rules:
//		1.Describe only what is visible in the image.
//2.Do NOT guess or assume identity, intent, or unseen details.
//3.Extract visible text accurately.
//4.Use clear, neutral, evidentiary language.
//5.If unsure, state the uncertainty.
//6.Never output chain-of - thought or internal reasoning.

//[USER QUESTION]
//{prompt}
//<|end|>
//<|assistant|>";

        string formattedPrompt = FormatPrompt(prompt);


		// Process images using the *formatted* prompt
		var tensors = _processor.ProcessImages(formattedPrompt, ortImages);

		using var stream = _processor.CreateStream();
		using var genParams = new GeneratorParams(_model);
		genParams.SetSearchOption("max_length", 3072);

	

		using var generator = new Generator(_model, genParams);
		generator.SetInputs(tensors);

		var result = new StringBuilder();

		while (!generator.IsDone())
		{
			generator.GenerateNextToken();
			var seq = generator.GetSequence(0)[^1];
			result.Append(stream.Decode(seq));
		}

		return result.ToString();
	}

	private static string FormatPrompt(string userPrompt)
	{
		return $"""
        <|user|>
        <|image_1|>
        You are CAILE-Vision, a digital forensics and OSINT image analyst.

        ANALYZE THIS IMAGE:

        1. TYPE: What kind of image is this? (screenshot, document, chat, photo, etc.)

        2. TEXT EXTRACTION: Transcribe ALL visible text exactly.
           - Preserve spelling, formatting, line breaks
           - Note language(s) detected
           - [illegible] for unreadable portions

        3. GEOLOCATION INDICATORS: Identify region-specific elements.
           - Electrical outlets/plug types (Type A, B, C, G, etc.)
           - Power socket design, voltage indicators
           - Vehicle license plates (format, color scheme)
           - Road signs (style, language, symbols)
           - Street furniture (bollards, crosswalk style, traffic lights)
           - Architecture style, building materials, roof types
           - Vegetation, landscape features
           - Currency, price formats, units of measurement
           - Language on signage, brands, products
           - Clothing styles, uniforms
           - Sun position/shadows (if relevant)
           - Phone number formats, country codes
           - Date formats (MM/DD vs DD/MM)
           - Driving side (left/right) if visible

        4. DIGITAL IDENTIFIERS:
           - Timestamps, dates, times, timezones
           - Usernames, emails, phone numbers
           - URLs, file paths, IP addresses
           - Application names, OS indicators
           - Device metadata visible in UI
           - Reference numbers, case IDs

        5. OBJECTS & CONTEXT:
           - Devices (make/model if identifiable)
           - Documents (type, visible letterhead)
           - People (count only, no identification)
           - Weapons, contraband, evidence items

        6. ANALYST NOTES:
           - Flag investigatively significant details
           - [UNCERTAIN] for unclear observations
           - [GEOLOC] for geolocation-relevant findings

        RULES:
        - Report ONLY what is directly visible
        - Do NOT identify individuals
        - Do NOT speculate on intent or context
        - BE SPECIFIC about shapes, colors, styles

        ANALYST QUESTION: {userPrompt}
        <|end|>
        <|assistant|>
        """;
	}

	public void Dispose()
	{
		_tokenizer?.Dispose();
		_processor?.Dispose();
		_model?.Dispose();
	}
}

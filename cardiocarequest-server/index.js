import express from "express";
import bodyParser from "body-parser";
import { Storage } from "@google-cloud/storage";
import { PDFDocument, rgb, StandardFonts } from "pdf-lib";
import nodemailer from "nodemailer";
import fs from "fs";
import path from "path";
import TinyURL from "tinyurl";

const app = express();

// Increase JSON payload limit to 50MB for base64 images
app.use(bodyParser.json({ limit: "50mb" }));
app.use(bodyParser.urlencoded({ extended: true, limit: "10mb" }));

// Google Cloud Storage setup
const storage = new Storage({
  projectId: "cardiocare-quest-telemetry",
  keyFilename: "./firebase-service-account.json",
});
const bucketName = "cardiocare-quest-telemetry.firebasestorage.app";
const bucket = storage.bucket(bucketName);

// Nodemailer setup 
const transporter = nodemailer.createTransport({
  service: "gmail",
  auth: {
    user: process.env.EMAIL_USER,
    pass: process.env.EMAIL_PASS,
  },
});

// Draw text on PDF page
function safeText(page, text, x, y, font, size = 12) {
  page.drawText(text || "", { x, y, font, size, color: rgb(0, 0, 0) });
}

// Embed Base64 PNG image on PDF page
async function safeImage(page, base64, x, y, pdfDoc, targetHeight = 28, widthFactor = 7 {
  if (!base64) return;
  try {
    const img = await pdfDoc.embedPng(base64);
    const dims = img.scale(1);

    // Scale image to match target height 
    const aspectRatio = dims.width / dims.height;
    const height = targetHeight;
    const width = targetHeight * aspectRatio * widthFactor;;

    page.drawImage(img, { x, y, width, height });
  } catch (e) {
    console.error(`Failed to embed signature image:`, e);
  }
}

app.post("/document-generation", async (req, res) => {
  try {
    const userInput = req.body;

    if (!userInput.name || !userInput.email || !userInput.signature) {
      return res.status(400).json({ success: false, error: `Missing required fields` });
    }

    // Download template PDF from GCS
    const tempFilePath = path.join("./", "ConsentForm.pdf");
    await bucket.file("ConsentForm.pdf").download({ destination: tempFilePath });

    const existingPdfBytes = fs.readFileSync(tempFilePath);
    const pdfDoc = await PDFDocument.load(existingPdfBytes);
    const pages = pdfDoc.getPages();
    const page = pages[4]; // page 5
    const font = await pdfDoc.embedFont(StandardFonts.Helvetica);

    // Insert text & signatures
    safeText(page, userInput.name, 90, 420, font);
    safeText(page, userInput.date || new Date().toLocaleDateString(), 450, 420, font);
    await safeImage(page, userInput.signature, 250, 420, pdfDoc);

    await safeImage(page, userInput.audioSignature || userInput.signature, 170, 305, pdfDoc);
    safeText(page, userInput.audioDate || userInput.date || new Date().toLocaleDateString(), 435, 310, font);

    await safeImage(page, userInput.videoSignature || userInput.signature, 170, 220, pdfDoc);
    safeText(page, userInput.videoDate || userInput.date || new Date().toLocaleDateString(), 435, 220, font);

    // Save updated PDF
    const pdfBytes = await pdfDoc.save();
    const outputFileName = `ConsentForm-${Date.now()}.pdf`;
    const file = bucket.file(outputFileName);
    await file.save(pdfBytes, { contentType: "application/pdf", resumable: false });

    // Generate signed URL
    const [signedUrl] = await file.getSignedUrl({
      action: "read",
      expires: "2500-01-01", // effectively permanent
    });

    // Shorten the URL
    let shortUrl = signedUrl;
    try {
      shortUrl = await TinyURL.shorten(signedUrl);
    } catch (shortErr) {
      console.warn(`Failed to shorten URL, using full URL instead:`, shortErr.message);
    }

    // Send email
    try {
      await transporter.sendMail({
        from: `"CardioCare Quest" <${process.env.EMAIL_USER}>`,
        to: userInput.email,
        subject: "Your Signed Consent Form",
        text: `Hi ${userInput.name},\n\nYour signed consent form is ready. Download link: ${shortUrl}`,
      });
    } catch (mailErr) {
      console.warn(`Failed to send email:`, mailErr.message);
    }

    res.json({ success: true, downloadUrl: shortUrl });

  } catch (err) {
    console.error(`Error generating PDF:`, err);
    res.status(500).json({
      error: `Failed to generate or send ConsentForm PDF`,
    });
  }
});

const PORT = process.env.PORT || 8080;
app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
});

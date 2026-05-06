# Azure Document Intelligence Setup Guide

## Overview
ReceiptWise uses **Azure Document Intelligence** (formerly Form Recognizer) to extract structured data from receipt images.

---

## Prerequisites
- Azure subscription (free tier available)
- Azure portal access

---

## Setup Steps

### 1. Create Azure Document Intelligence Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **"Create a resource"**
3. Search for **"Document Intelligence"** (or "Form Recognizer")
4. Click **"Create"**

### 2. Configure Resource

- **Subscription**: Select your subscription
- **Resource Group**: Create new or use existing
- **Region**: Choose closest to your users (e.g., `East US`, `West Europe`)
- **Name**: `receiptwise-doc-intelligence` (or your choice)
- **Pricing Tier**: 
  - **Free (F0)**: 500 pages/month, 20 requests/min
  - **Standard (S0)**: Pay-as-you-go, higher limits

### 3. Get Credentials

After deployment:
1. Go to your resource
2. Navigate to **"Keys and Endpoint"** (left menu)
3. Copy:
   - **Endpoint**: `https://YOUR_RESOURCE.cognitiveservices.azure.com/`
   - **Key 1**: `xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

---

## Configure in ReceiptWise

### Method 1: Via App Settings (Development)

1. Open ReceiptWise app
2. Go to **Settings** tab
3. Tap **"Configure Azure Credentials"**
4. Paste:
   - Endpoint URL
   - API Key
5. Restart app

### Method 2: Via Preferences (Programmatic)
Preferences.Set("AzureDocIntelligence_Endpoint", "https://YOUR_RESOURCE.cognitiveservices.azure.com/"); Preferences.Set("AzureDocIntelligence_ApiKey", "YOUR_API_KEY");

### Method 3: Production (Backend API)

**Recommended for production:**
- Store keys server-side
- App calls your backend API
- Backend proxies requests to Azure
- Never expose keys in mobile app

---

## Testing the Integration

### 1. Test with Sample Receipt

1. Capture or import a receipt image
2. Tap **"Extract with AI"**
3. Wait for processing (5-15 seconds)
4. Verify extracted data:
   - Merchant name
   - Transaction date
   - Total amount
   - Tax
   - Line items

### 2. Check Logs

Enable debug logging to see extraction details:
builder.Logging.AddDebug();


Look for:
- `"Receipt extraction successful"`
- Field values and confidence scores

---

## Supported Document Types

### Images
- JPEG
- PNG
- BMP
- TIFF

### Files
- PDF (up to 2000 pages)

### Size Limits
- Max file size: 50 MB
- Max resolution: 10,000 x 10,000 px

---

## Pricing

### Free Tier (F0)
- 500 pages/month
- 20 API calls/minute
- Perfect for development and testing

### Standard Tier (S0)
- $1.50 per 1,000 pages (first 1M pages)
- $0.60 per 1,000 pages (1M-10M pages)
- $0.40 per 1,000 pages (10M+ pages)

**Receipt Wise Estimate:**
- 100 receipts/month = ~$0.15/month
- 1,000 receipts/month = ~$1.50/month

---

## Troubleshooting

### Error: "Authentication failed"
**Cause:** Invalid API key  
**Solution:** Double-check key in Azure portal

### Error: "Rate limit exceeded"
**Cause:** Too many requests (Free tier: 20/min)  
**Solution:** 
- Wait 60 seconds
- Upgrade to Standard tier

### Error: "Resource not found"
**Cause:** Incorrect endpoint URL  
**Solution:** Verify endpoint matches your resource

### Low Confidence Scores
**Cause:** Poor image quality  
**Solution:**
- Use well-lit photos
- Avoid shadows
- Ensure text is readable
- Keep receipt flat

### Extraction Returns Empty Data
**Cause:** Document Intelligence couldn't detect receipt  
**Solution:**
- Verify receipt format is standard
- Check image isn't rotated
- Try different receipt

---

## Security Best Practices

### ✅ DO:
- Store keys in SecureStorage (mobile)
- Use environment variables (backend)
- Rotate keys regularly
- Use separate keys for dev/prod
- Monitor usage in Azure portal

### ❌ DON'T:
- Hardcode keys in source code
- Commit keys to Git
- Share keys in public channels
- Use production keys in dev builds

---

## Advanced Features

### Custom Models (Future)
Train custom models for:
- Store-specific receipts
- Non-English receipts
- Specialized invoice formats

### Batch Processing
Process multiple receipts:
var tasks = files.Select(f => ProcessReceiptAsync(f)); await Task.WhenAll(tasks);

### Confidence Thresholds
Flag low-confidence extractions:
if (result.Confidence < 0.7f) { // Request manual review }


---

## References

- [Azure Document Intelligence Docs](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/)
- [Prebuilt Receipt Model](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/concept-receipt)
- [Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/)


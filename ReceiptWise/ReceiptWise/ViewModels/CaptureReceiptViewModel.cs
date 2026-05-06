namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Core.Enums;
using ReceiptWise.Services.Helpers;

/// <summary>
/// ViewModel for Capture Receipt page
/// Handles camera capture and file import
/// </summary>
public partial class CaptureReceiptViewModel : BaseViewModel
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ImageHelper _imageHelper;

    [ObservableProperty]
    private ImageSource? _capturedImage;

    [ObservableProperty]
    private string? _capturedFilePath;

    [ObservableProperty]
    private bool _hasImage;

    [ObservableProperty]
    private string _statusMessage = "Ready to capture";

    public CaptureReceiptViewModel(
        IFileStorageService fileStorageService,
        IReceiptRepository receiptRepository,
        IAttachmentRepository attachmentRepository,
        ImageHelper imageHelper)
    {
        _fileStorageService = fileStorageService;
        _receiptRepository = receiptRepository;
        _attachmentRepository = attachmentRepository;
        _imageHelper = imageHelper;
        Title = "Capture Receipt";
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();
            StatusMessage = "Opening camera...";

            // Check camera permissions
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    SetError("Camera permission is required to capture receipts");
                    StatusMessage = "Permission denied";
                    return;
                }
            }

            // Capture photo
            var photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Capture Receipt"
            });

            if (photo != null)
            {
                await ProcessCapturedFileAsync(photo);
            }
            else
            {
                StatusMessage = "Capture cancelled";
            }
        }
        catch (FeatureNotSupportedException)
        {
            SetError("Camera is not supported on this device");
            StatusMessage = "Camera not available";
        }
        catch (PermissionException)
        {
            SetError("Camera permission denied");
            StatusMessage = "Permission denied";
        }
        catch (Exception ex)
        {
            SetError($"Failed to capture photo: {ex.Message}");
            StatusMessage = "Capture failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();
            StatusMessage = "Opening file picker...";

            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.image", "com.adobe.pdf" } },
                { DevicePlatform.Android, new[] { "image/*", "application/pdf" } },
                { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" } },
                { DevicePlatform.MacCatalyst, new[] { "public.image", "com.adobe.pdf" } }
            });

            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select Receipt Image or PDF",
                FileTypes = customFileType
            });

            if (result != null)
            {
                await ProcessCapturedFileAsync(result);
            }
            else
            {
                StatusMessage = "File selection cancelled";
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to pick file: {ex.Message}");
            StatusMessage = "File selection failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ProcessCapturedFileAsync(FileResult fileResult)
    {
        try
        {
            StatusMessage = "Processing file...";

            var stream = await fileResult.OpenReadAsync();
            var fileName = fileResult.FileName;
            var mimeType = ImageHelper.GetMimeType(fileName);

            // Validate if it's an image (skip PDF validation for now)
            if (mimeType.StartsWith("image/") && !_imageHelper.IsValidImage(stream))
            {
                SetError("Invalid image file");
                StatusMessage = "Invalid file";
                return;
            }

            // Compress image if needed
            if (mimeType.StartsWith("image/"))
            {
                stream = await _imageHelper.CompressImageAsync(stream);
            }

            // Save file to local storage
            var savedPath = await _fileStorageService.SaveFileAsync(stream, fileName);

            // Generate thumbnail for images
            string? thumbnailPath = null;
            if (mimeType.StartsWith("image/"))
            {
                var thumbnailStream = await _imageHelper.GenerateThumbnailAsync(stream);
                thumbnailPath = await _fileStorageService.SaveThumbnailAsync(thumbnailStream, fileName);
            }

            // Update UI
            CapturedFilePath = savedPath;
            CapturedImage = ImageSource.FromFile(savedPath);
            HasImage = true;
            StatusMessage = $"File captured: {fileName}";

            await Shell.Current.DisplayAlert(
                "Success",
                "Receipt captured! Next: AI extraction will be implemented in Milestone 4.",
                "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to process file: {ex.Message}");
            StatusMessage = "Processing failed";
        }
    }

    [RelayCommand]
    private async Task SaveManualReceiptAsync()
    {
        // Placeholder for manual entry (Milestone 6)
        if (!HasImage)
        {
            await Shell.Current.DisplayAlert("No Image", "Please capture or import a receipt first", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Saving receipt...";

            // Create a manual receipt entry
            var receipt = new Receipt
            {
                MerchantName = "Manual Entry",
                TransactionDate = DateTime.Now,
                Total = 0,
                Tax = 0,
                Subtotal = 0,
                Currency = CurrencyCode.USD,
                Category = ReceiptCategory.Other,
                ExtractionStatus = ExtractionStatus.Pending,
                Notes = "Captured image - awaiting AI extraction"
            };

            var receiptId = await _receiptRepository.AddAsync(receipt);

            // Save attachment
            if (!string.IsNullOrEmpty(CapturedFilePath))
            {
                var attachment = new Attachment
                {
                    ReceiptId = receiptId,
                    FileName = Path.GetFileName(CapturedFilePath),
                    FilePath = CapturedFilePath,
                    FileType = ImageHelper.GetMimeType(CapturedFilePath),
                    FileSizeBytes = new FileInfo(CapturedFilePath).Length
                };

                await _attachmentRepository.AddAsync(attachment);
            }

            await Shell.Current.DisplayAlert("Success", "Receipt saved! AI extraction coming in Milestone 4.", "OK");

            // Reset form
            CapturedImage = null;
            CapturedFilePath = null;
            HasImage = false;
            StatusMessage = "Ready to capture";
        }
        catch (Exception ex)
        {
            SetError($"Failed to save receipt: {ex.Message}");
            StatusMessage = "Save failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearCapture()
    {
        CapturedImage = null;
        CapturedFilePath = null;
        HasImage = false;
        StatusMessage = "Ready to capture";
        ClearError();
    }
}
namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Data.Repositories;
using ReceiptWise.Services.Business;
using ReceiptWise.Services.Helpers;
using ReceiptWise.Services.Infrastructure;

/// <summary>
/// ViewModel for Capture Receipt page
/// Enhanced with AI extraction
/// </summary>
public partial class CaptureReceiptViewModel : BaseViewModel
{
    //private readonly ReceiptProcessingService _processingService;
    //private readonly ImageHelper _imageHelper;
    //private readonly IFileStorageService _fileStorageService;
    //private readonly IReceiptRepository _receiptRepository;
    //private readonly IAttachmentRepository _attachmentRepository;

    //[ObservableProperty]
    //private ImageSource? _capturedImage;

    //[ObservableProperty]
    //private string? _capturedFilePath;

    //[ObservableProperty]
    //private FileResult? _capturedFile;

    //[ObservableProperty]
    //private bool _hasImage;

    //[ObservableProperty]
    //private string _statusMessage = "Ready to capture";

    //[ObservableProperty]
    //private bool _isProcessing;

    //[ObservableProperty]
    //private int _processingProgress; // 0-100

    //private readonly ImageOptimizationService _imageOptimizationService;

    //public CaptureReceiptViewModel(
    //    IFileStorageService fileStorageService,
    //    IReceiptRepository receiptRepository,
    //    IAttachmentRepository attachmentRepository,
    //    ImageHelper imageHelper,
    //    ReceiptProcessingService processingService,
    //    ImageOptimizationService imageOptimizationService) // Add this
    //{
    //    _fileStorageService = fileStorageService;
    //    _receiptRepository = receiptRepository;
    //    _attachmentRepository = attachmentRepository;
    //    _imageHelper = imageHelper;
    //    _processingService = processingService;
    //    _imageOptimizationService = imageOptimizationService; // Add this
    //    Title = "Capture Receipt";
    //}
    private readonly IFileStorageService _fileStorageService;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ReceiptProcessingService _processingService;
    private readonly ImageHelper _imageHelper;

    [ObservableProperty]
    private ImageSource? _capturedImage;

    [ObservableProperty]
    private string? _capturedFilePath;

    [ObservableProperty]
    private FileResult? _capturedFile;

    [ObservableProperty]
    private bool _hasImage;

    [ObservableProperty]
    private string _statusMessage = "Ready to capture";

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private int _processingProgress; // 0-100

    private readonly ImageOptimizationService _imageOptimizationService;

    public CaptureReceiptViewModel(
        IFileStorageService fileStorageService,
        IReceiptRepository receiptRepository,
        IAttachmentRepository attachmentRepository,
        ImageHelper imageHelper,
        ImageOptimizationService imageOptimizationService,
        ReceiptProcessingService processingService) // Add this parameter
    {
        _fileStorageService = fileStorageService;
        _receiptRepository = receiptRepository;
        _attachmentRepository = attachmentRepository;
        _imageHelper = imageHelper;
        _imageOptimizationService = imageOptimizationService;
        _processingService = processingService; // Add this assignment
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

    // Update the ProcessCapturedFileAsync method to use optimized images

    private async Task ProcessCapturedFileAsync(FileResult fileResult)
    {
        try
        {
            StatusMessage = "Processing file...";

            var stream = await fileResult.OpenReadAsync();
            var fileName = fileResult.FileName;
            var mimeType = ImageHelper.GetMimeType(fileName);

            // Validate if it's an image
            if (mimeType.StartsWith("image/") && !_imageHelper.IsValidImage(stream))
            {
                SetError("Invalid image file");
                StatusMessage = "Invalid file";
                return;
            }

            // Auto-rotate image
            if (mimeType.StartsWith("image/"))
            {
                StatusMessage = "Optimizing image...";
                stream = await _imageOptimizationService.AutoRotateImageAsync(stream);

                // Compress image
                stream = await _imageOptimizationService.CompressImageAsync(stream);
            }

            // Store for preview
            CapturedFile = fileResult;
            stream.Position = 0;

            if (mimeType.StartsWith("image/"))
            {
                var bytes = new byte[stream.Length];
                await stream.ReadAsync(bytes);
                CapturedImage = ImageSource.FromStream(() => new MemoryStream(bytes));
            }

            HasImage = true;
            StatusMessage = $"File ready: {fileName}";
        }
        catch (Exception ex)
        {
            SetError($"Failed to process file: {ex.Message}");
            StatusMessage = "Processing failed";
        }
    }

    [RelayCommand]
    private async Task ProcessWithAIAsync()
    {
        if (CapturedFile == null)
        {
            await Shell.Current.DisplayAlert("No Image", "Please capture or import a receipt first", "OK");
            return;
        }

        try
        {
            IsProcessing = true;
            IsBusy = true;
            ProcessingProgress = 0;
            ClearError();

            StatusMessage = "🤖 Extracting data with AI...";
            ProcessingProgress = 25;

            var stream = await CapturedFile.OpenReadAsync();

            ProcessingProgress = 50;
            StatusMessage = "📊 Analyzing receipt...";

            var result = await _processingService.ProcessReceiptAsync(
                stream,
                CapturedFile.FileName);

            ProcessingProgress = 100;

            if (result.Success)
            {
                StatusMessage = "✅ Receipt processed successfully!";

                var extraction = result.ExtractionResult;
                var message = $"Merchant: {extraction?.MerchantName}\n" +
                             $"Total: ${extraction?.Total:F2}\n" +
                             $"Date: {extraction?.TransactionDate:MMM dd, yyyy}\n" +
                             $"Items: {extraction?.Items.Count ?? 0}\n" +
                             $"Confidence: {extraction?.Confidence:P0}";

                await Shell.Current.DisplayAlert("Success!", message, "OK");

                // Navigate to receipt detail
                await Shell.Current.GoToAsync($"//ReceiptListPage");
            }
            else
            {
                StatusMessage = $"⚠️ {result.ErrorMessage}";
                SetError(result.ErrorMessage ?? "Processing failed");

                if (result.Status == ProcessingStatus.SavedAsManual)
                {
                    await Shell.Current.DisplayAlert(
                        "Saved as Manual Entry",
                        "AI extraction failed, but the receipt was saved. You can edit it manually from the receipts list.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            SetError($"AI processing failed: {ex.Message}");
            StatusMessage = "❌ Processing failed";
        }
        finally
        {
            IsProcessing = false;
            IsBusy = false;
            ProcessingProgress = 0;
        }
    }

    [RelayCommand]
    private void ClearCapture()
    {
        CapturedImage = null;
        CapturedFilePath = null;
        CapturedFile = null;
        HasImage = false;
        ProcessingProgress = 0;
        StatusMessage = "Ready to capture";
        ClearError();
    }
}
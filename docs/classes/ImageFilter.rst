.. include:: ../header.rst

.. _ImageFilter:

================
ImageFilter 
================

The `ImageFilter` class provides methods to work with image filters applied a PDF document.

=========================================== ===================================================
**Method / Attribute**                      **Short Description**
=========================================== ===================================================
:meth:`ImageFilter.AutoDeskew`              Automatically detects and corrects image skew (rotation).
:meth:`ImageFilter.ApplyDilation`           Applies dilation filter to correct broken letters.                   
:meth:`ImageFilter.RemoveLines`             Removes lines from the image (either horizontal or vertical).  
:meth:`ImageFilter.RemoveVerticalLines`     Removes vertical lines from the image.              
:meth:`ImageFilter.RemoveHorizontalLines`   Removes horizontal lines from the image.              
:meth:`ImageFilter.CollectLines`            Detects and collects line segments from the image without removing them.          
:meth:`ImageFilter.ApplyMedian`             Applies median filter to reduce noise in the image.      
:meth:`ImageFilter.AdjustGamma`             Adjusts the gamma correction of the image.        
:meth:`ImageFilter.AdjustContrast`          Adjusts the contrast of the image.               
:meth:`ImageFilter.ApplyBlur`               Applies blur filter to the image.                  
:meth:`ImageFilter.ToGrayscale`             Converts the image to grayscale.                
:meth:`ImageFilter.ApplyInvert`             Inverts the colors of the image.                            
:meth:`ImageFilter.ScaleImage`              Scales the image by the specified factor while maintaining aspect ratio.                
:meth:`ImageFilter.Fit`                     Scales the image to fit within the specified maximum dimension while maintaining aspect ratio.                  
:meth:`ImageFilter.ApplyFilters`            Applies a sequence of image processing filters to the image in the order specified.                  
:meth:`ImageFilter.EnsureColorType`         Ensures the bitmap has the specified color type and alpha type.                

=========================================== ===================================================

**Class API**

.. class:: ImageFilter

    .. method:: ImageFilter()

      Instantiation method.



    .. method:: AutoDeskew(ref SKBitmap image, double minAngle = DeskewFilterOptions.DEFAULT_TILT_CORRECTION_ANGLE_THRESHOLD)

        Automatically detects and corrects image skew (rotation) using projection-based analysis.

        :param image: The image to deskew. Will be replaced with the corrected image.
        :type image: SKBitmap
        :param minAngle: Minimum angle in degrees required to apply correction. Default is 0.4 degrees.
        :type minAngle: double
        :raises ArgumentNullException: Thrown when image is null.
        :return: None (modifies image in place via ref parameter)

    .. method:: ApplyDilation(ref SKBitmap image)

        Applies dilation filter to correct broken letters by expanding dark pixels.
        Useful for repairing fragmented text in scanned documents.

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :raises ArgumentNullException: Thrown when image is null.
        :return: None (modifies image in place via ref parameter)

    .. method:: RemoveLines(ref SKBitmap image, bool vertical)

        Removes lines from the image (either horizontal or vertical).
        Uses advanced algorithms to preserve text while removing table borders and separator lines.

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :param vertical: True to remove vertical lines, false to remove horizontal lines.
        :type vertical: bool
        :raises ArgumentNullException: Thrown when image is null.
        :return: None (modifies image in place via ref parameter)

    .. method:: RemoveVerticalLines(ref SKBitmap image)

        Removes vertical lines from the image (table borders, separators, etc.).

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :raises ArgumentNullException: Thrown when image is null.
        :return: None (modifies image in place via ref parameter)

    .. method:: RemoveHorizontalLines(ref SKBitmap image)

        Removes horizontal lines from the image (table borders, separators, etc.).

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :raises ArgumentNullException: Thrown when image is null.
        :return: None (modifies image in place via ref parameter)

    .. method:: CollectLines(ref SKBitmap image)

        Detects and collects line segments from the image without removing them.
        Returns both horizontal and vertical line segments found in the image.

        :param image: The image to analyze.
        :type image: SKBitmap
        :raises ArgumentNullException: Thrown when image is null.
        :return: A tuple containing lists of horizontal and vertical line segments.
        :rtype: (List<LineRemover.Segment> Horizontal, List<LineRemover.Segment> Vertical)

    .. method:: CollectLines(ref SKBitmap image, ref List<LineRemover.Segment> horizontal, ref List<LineRemover.Segment> vertical)

        Detects and collects line segments from the image into provided lists.

        :param image: The image to analyze.
        :type image: SKBitmap
        :param horizontal: List to populate with horizontal line segments. Will be created if null.
        :type horizontal: List<LineRemover.Segment>
        :param vertical: List to populate with vertical line segments. Will be created if null.
        :type vertical: List<LineRemover.Segment>
        :raises ArgumentNullException: Thrown when image is null.
        :return: None (modifies horizontal and vertical lists in place via ref parameters)


    .. method:: ApplyMedian(ref SKBitmap image, int kernelSize = 3)

        Applies median filter to reduce noise in the image.
        Each pixel is replaced with the median value of its neighboring pixels.

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :param kernelSize: Size of the median filter kernel (must be odd). Default is 3.
        :type kernelSize: int
        :raises ArgumentNullException: Thrown when image is null.
        :raises ArgumentOutOfRangeException: Thrown when kernelSize is not positive.
        :return: None (modifies image in place via ref parameter)

        .. note::
            If kernelSize is even, it will be automatically incremented by 1 to make it odd.

    .. method:: AdjustGamma(ref SKBitmap image, double gamma)

        Adjusts the gamma correction of the image.
        Gamma values less than 1.0 darken the image, values greater than 1.0 brighten it.

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :param gamma: Gamma correction value. Must be greater than zero. 1.0 means no change.
        :type gamma: double
        :raises ArgumentNullException: Thrown when image is null.
        :raises ArgumentOutOfRangeException: Thrown when gamma is not greater than zero.
        :return: None (modifies image in place via ref parameter)

    .. method:: AdjustContrast(ref SKBitmap image, int contrastLevel)

        Adjusts the contrast of the image.
        Requires RGB color format. The image will be converted if necessary.

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :param contrastLevel: Contrast adjustment from -100 to +100. 0 means no change.
        :type contrastLevel: int
        :raises ArgumentNullException: Thrown when image is null.
        :return: True if the operation succeeded, false if the image format is not supported.
        :rtype: bool

    .. method:: ApplyBlur(ref SKBitmap image, int blurZoneSize)

        Applies blur filter to the image using a box blur algorithm.
        Requires RGB color format. The image will be converted if necessary.

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :param blurZoneSize: Size of the blur zone (1-10). Larger values create more blur.
        :type blurZoneSize: int
        :raises ArgumentNullException: Thrown when image is null.
        :raises ArgumentOutOfRangeException: Thrown when blurZoneSize is less than 1.
        :return: None (modifies image in place via ref parameter)

    .. method:: ToGrayscale(ref SKBitmap image)

        Converts the image to grayscale using standard RGB weights (0.299R + 0.587G + 0.114B).

        :param image: The image to process. Will be replaced with the grayscale image.
        :type image: SKBitmap
        :raises ArgumentNullException: Thrown when image is null.
        :return: None (modifies image in place via ref parameter)

    .. method:: ApplyInvert(ref SKBitmap image)

        Inverts the colors of the image (creates a negative effect).

        :param image: The image to process. Will be replaced with the inverted image.
        :type image: SKBitmap
        :raises ArgumentNullException: Thrown when image is null.
        :return: None (modifies image in place via ref parameter)



    .. method:: ScaleImage(ref SKBitmap image, double scaleFactor, SKFilterQuality quality = SKFilterQuality.High)

        Scales the image by the specified factor while maintaining aspect ratio.

        :param image: The image to scale. Will be replaced with the scaled image.
        :type image: SKBitmap
        :param scaleFactor: Scale factor (e.g., 0.5 for half size, 2.0 for double size). Must be greater than zero.
        :type scaleFactor: double
        :param quality: Filter quality for scaling interpolation. Default is High.
        :type quality: SKFilterQuality
        :raises ArgumentNullException: Thrown when image is null.
        :raises ArgumentOutOfRangeException: Thrown when scaleFactor is not greater than zero.
        :return: The new size of the scaled image.
        :rtype: SKSizeI

        .. note::
            If scaleFactor is 1.0, the image remains unchanged and its current size is returned.    

    .. method:: Fit(ref SKBitmap image, int maxDimension, SKFilterQuality quality = SKFilterQuality.High)

        Scales the image to fit within the specified maximum dimension while maintaining aspect ratio.
        If the image is already smaller than maxDimension, no scaling is performed.

        :param image: The image to scale. Will be replaced with the scaled image.
        :type image: SKBitmap
        :param maxDimension: Maximum width or height in pixels. Must be greater than zero.
        :type maxDimension: int
        :param quality: Filter quality for scaling interpolation. Default is High.
        :type quality: SKFilterQuality
        :raises ArgumentNullException: Thrown when image is null.
        :raises ArgumentOutOfRangeException: Thrown when maxDimension is not greater than zero.
        :return: The new size of the scaled image.
        :rtype: SKSizeI

        .. note::
            The image is scaled proportionally to fit within a square of maxDimension × maxDimension pixels.
            If the image is already smaller than this size, it remains unchanged.
            

    .. function:: ApplyFilters(ref SKBitmap image, IEnumerable<PreprocessingFilter> filters)

        Applies a sequence of image processing filters to the image in the order specified.
        Each filter is applied sequentially, modifying the image in-place.

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :param filters: Collection of preprocessing filters to apply. Null or empty collection is ignored.
        :type filters: IEnumerable<PreprocessingFilter>
        :raises ArgumentNullException: Thrown when image is null.
        :raises NotSupportedException: Thrown when an unsupported filter type is encountered.
        :return: None (modifies image in place via ref parameter)

        .. note::
            Null filters within the collection are automatically skipped during processing.

        **Supported Filter Types:**

        - **Deskew**: Automatically detects and corrects image skew: ``ImageProcessingFilterType.Deskew``
        - **Dilate**: Expands dark pixels to repair broken letters: ``ImageProcessingFilterType.Dilate``
        - **RemoveVerticalLines**: Removes vertical lines from the image: ``ImageProcessingFilterType.RemoveVerticalLines``
        - **RemoveHorizontalLines**: Removes horizontal lines from the image: ``ImageProcessingFilterType.RemoveHorizontalLines``
        - **Median**: Applies median filter to reduce noise: ``ImageProcessingFilterType.Median``
        - **Gamma**: Adjusts gamma correction: ``ImageProcessingFilterType.Gamma``
        - **Contrast**: Adjusts image contrast: ``ImageProcessingFilterType.Contrast``
        - **Grayscale**: Converts image to grayscale: ``ImageProcessingFilterType.Grayscale``
        - **Invert**: Inverts image colors: ``ImageProcessingFilterType.Invert``
        - **Scale**: Scales image by a specified factor: ``ImageProcessingFilterType.Scale``
        - **Fit**: Scales image to fit within maximum dimensions: ``ImageProcessingFilterType.Fit`` 



    .. method:: EnsureColorType(ref SKBitmap bitmap, SKColorType colorType, SKAlphaType? alphaType = null)

        Ensures the bitmap has the specified color type and alpha type.
        Converts the bitmap if necessary, disposing the original and replacing it with the converted version.

        :param bitmap: The bitmap to check and convert if needed. Will be replaced if conversion is necessary.
        :type bitmap: SKBitmap
        :param colorType: The required color type.
        :type colorType: SKColorType
        :param alphaType: The required alpha type. If null, the original alpha type is preserved.
        :type alphaType: SKAlphaType?
        :return: None (modifies bitmap in place via ref parameter if conversion is needed)

        .. note::
            If the bitmap already has the specified color type and alpha type, no conversion is performed.
            When conversion occurs, the original bitmap is disposed and replaced with the converted version.
            
    
.. include:: ../footer.rst

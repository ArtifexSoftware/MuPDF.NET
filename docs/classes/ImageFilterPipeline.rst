.. include:: ../header.rst

.. _ImageFilterPipeline:

================
ImageFilterPipeline 
================

The `ImageFilterPipeline` class provide a Helper that lets callers compose and reuse preprocessing filter pipelines.

========================================================= ===================================================
**Method / Attribute**                                    **Short Description**
========================================================= ===================================================
:meth:`ImageFilterPipeline.Clear`                         Removes every filter from the pipeline.
:meth:`ImageFilterPipeline.RemoveAll`                     Removes all filters matching the specified type.
:meth:`ImageFilterPipeline.AddFilter`                     Adds a filter to the pipeline.
:meth:`ImageFilterPipeline.AddDeskew`                     Adds a deskew filter to the pipeline.
:meth:`ImageFilterPipeline.AddDilation`                   Adds a dilation filter to the pipeline.
:meth:`ImageFilterPipeline.AddRemoveVerticalLines`        Adds a remove vertical lines filter to the pipeline.
:meth:`ImageFilterPipeline.AddRemoveHoriziontalLines`     Adds a remove horizontal lines filter to the pipeline.
:meth:`ImageFilterPipeline.AddMedian`                     Adds a median filter to the pipeline.
:meth:`ImageFilterPipeline.AddGamma`                      Adds a gamma correction filter to the pipeline.
:meth:`ImageFilterPipeline.AddContrast`                   Adds a contrast adjustment filter to the pipeline.
:meth:`ImageFilterPipeline.AddGrayscale`                  Adds a grayscale conversion filter to the pipeline.
:meth:`ImageFilterPipeline.AddInvert`                     Adds a color inversion filter to the pipeline.
:meth:`ImageFilterPipeline.AddScale`                      Adds a scale filter to the pipeline.
:meth:`ImageFilterPipeline.AddScaleFit`                   Adds a scale-to-fit filter to the pipeline.
:meth:`ImageFilterPipeline.Apply`                         Applies the configured filters to the supplied image.
              

========================================================= ===================================================

**Class API**

.. class:: ImageFilterPipeline
    
 
    .. property:: Filters

        Gets a read-only view of the configured filters, in execution order.

        :type: IReadOnlyList<PreprocessingFilter>
        :access: Read-only

    .. method:: Clear()

        Removes every filter from the pipeline.

        :return: None

    .. method:: RemoveAll(ImageProcessingFilterType type)

        Removes all filters matching the specified type.

        :param type: The filter type to remove from the pipeline.
        :type type: ImageProcessingFilterType
        :return: None

        .. note::
            Null filters are automatically excluded during the removal process.


    .. method:: AddFilter(ImageProcessingFilterType type, IImageProcessingFilterOptions options = null, bool replaceExisting = false)

        Adds a filter to the pipeline.

        :param type: The type of filter to add.
        :type type: ImageProcessingFilterType
        :param options: Optional configuration options for the filter. Default is null.
        :type options: IImageProcessingFilterOptions
        :param replaceExisting: If true, removes all existing filters of the same type before adding. Default is false.
        :type replaceExisting: bool
        :return: None

    .. method:: AddFilter(PreprocessingFilter filter, bool replaceExisting = false)

        Adds an already constructed PreprocessingFilter.

        :param filter: The preprocessing filter to add to the pipeline.
        :type filter: PreprocessingFilter
        :param replaceExisting: If true, removes all existing filters of the same type before adding. Default is false.
        :type replaceExisting: bool
        :raises ArgumentNullException: Thrown when filter is null.
        :return: None

    .. method:: AddDeskew(double minAngle = DeskewFilterOptions.DEFAULT_TILT_CORRECTION_ANGLE_THRESHOLD, bool replaceExisting = true)

        Adds a deskew filter to the pipeline.

        :param minAngle: Minimum angle in degrees required to apply correction. Default is the default tilt correction angle threshold.
        :type minAngle: double
        :param replaceExisting: If true, removes all existing deskew filters before adding. Default is true.
        :type replaceExisting: bool
        :raises ArgumentOutOfRangeException: Thrown when minAngle is negative.
        :return: None

    .. method:: AddDilation(bool replaceExisting = false)

        Adds a dilation filter to the pipeline.

        :param replaceExisting: If true, removes all existing dilation filters before adding. Default is false.
        :type replaceExisting: bool
        :return: None

    .. method:: AddRemoveVerticalLines(bool replaceExisting = true)

        Adds a remove vertical lines filter to the pipeline.

        :param replaceExisting: If true, removes all existing remove vertical lines filters before adding. Default is true.
        :type replaceExisting: bool
        :return: None

    .. method:: AddRemoveHoriziontalLines(bool replaceExisting = true)

        Adds a remove horizontal lines filter to the pipeline.

        :param replaceExisting: If true, removes all existing remove horizontal lines filters before adding. Default is true.
        :type replaceExisting: bool
        :return: None

    .. method:: AddMedian(int blockSize, bool replaceExisting = false)

        Adds a median filter to the pipeline.

        :param blockSize: Size of the median filter kernel. Must be positive.
        :type blockSize: int
        :param replaceExisting: If true, removes all existing median filters before adding. Default is false.
        :type replaceExisting: bool
        :raises ArgumentOutOfRangeException: Thrown when blockSize is not positive.
        :return: None

    .. method:: AddGamma(double gamma, bool replaceExisting = true)

        Adds a gamma correction filter to the pipeline.

        :param gamma: Gamma correction value. Must be greater than zero. Values less than 1.0 darken the image, values greater than 1.0 brighten it.
        :type gamma: double
        :param replaceExisting: If true, removes all existing gamma filters before adding. Default is true.
        :type replaceExisting: bool
        :raises ArgumentOutOfRangeException: Thrown when gamma is not greater than zero.
        :return: None

    .. method:: AddContrast(int contrast, bool replaceExisting = true)

        Adds a contrast adjustment filter to the pipeline.

        :param contrast: Contrast adjustment level from -100 to +100. 0 means no change.
        :type contrast: int
        :param replaceExisting: If true, removes all existing contrast filters before adding. Default is true.
        :type replaceExisting: bool
        :return: None

    .. method:: AddGrayscale(bool replaceExisting = true)

        Adds a grayscale conversion filter to the pipeline.

        :param replaceExisting: If true, removes all existing grayscale filters before adding. Default is true.
        :type replaceExisting: bool
        :return: None

    .. method:: AddInvert(bool replaceExisting = true)

        Adds a color inversion filter to the pipeline.

        :param replaceExisting: If true, removes all existing invert filters before adding. Default is true.
        :type replaceExisting: bool
        :return: None

    .. method:: AddScale(double scaleFactor, SKFilterQuality quality = SKFilterQuality.High, bool replaceExisting = false)

        Adds a scale filter to the pipeline.

        :param scaleFactor: Scale factor (e.g., 0.5 for half size, 2.0 for double size). Must be greater than zero.
        :type scaleFactor: double
        :param quality: Filter quality for scaling interpolation. Default is High.
        :type quality: SKFilterQuality
        :param replaceExisting: If true, removes all existing scale filters before adding. Default is false.
        :type replaceExisting: bool
        :raises ArgumentOutOfRangeException: Thrown when scaleFactor is not greater than zero.
        :return: None



    .. method:: AddScaleFit(int maxDimension, SKFilterQuality quality = SKFilterQuality.High, bool replaceExisting = true)

        Adds a scale-to-fit filter to the pipeline.

        :param maxDimension: Maximum width or height in pixels. Must be greater than zero.
        :type maxDimension: int
        :param quality: Filter quality for scaling interpolation. Default is High.
        :type quality: SKFilterQuality
        :param replaceExisting: If true, removes all existing fit filters before adding. Default is true.
        :type replaceExisting: bool
        :raises ArgumentOutOfRangeException: Thrown when maxDimension is not greater than zero.
        :return: None

        .. note::
            The image will be scaled to fit within a square of maxDimension * maxDimension pixels while maintaining aspect ratio.  

    .. method:: Apply(ref SKBitmap image)

        Applies the configured filters to the supplied image.

        :param image: The image to process. Will be replaced with the processed image.
        :type image: SKBitmap
        :return: None (modifies image in place via ref parameter)

        .. note::
            Filters are applied sequentially in the order they were added to the pipeline.                      

.. include:: ../footer.rst
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Volume))]
public class VolumeFxProxy : MonoBehaviour
{
    [SerializeField] private Volume _volume;

    [Header("Bloom")]
    public float bloomThreshold = 1f;
    public float bloomIntensity = 0f;
    public float bloomScatter = 0.5f;
    public Color bloomTint = Color.white;
    public float bloomDirtIntensity = 0f;

    [Header("Color Adjustments")]
    public float postExposure = 0f;
    public float contrast = 0f;
    public Color colorFilter = Color.white;
    public float hueShift = 0f;
    public float saturation = 0f;

    [Header("White Balance")]
    public float whiteTemperature = 0f;
    public float whiteTint = 0f;

    [Header("Split Toning")]
    public Color splitShadows = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color splitHighlights = new Color(0.5f, 0.5f, 0.5f, 1f);
    public float splitBalance = 0f;

    [Header("Lens Distortion")]
    public float lensIntensity = 0f;
    public float lensXMultiplier = 1f;
    public float lensYMultiplier = 1f;
    public float lensCenterX = 0.5f;
    public float lensCenterY = 0.5f;
    public float lensScale = 1f;

    [Header("Chromatic Aberration")]
    public float chromaIntensity = 0f;

    [Header("Vignette")]
    public Color vignetteColor = Color.black;
    public float vignetteCenterX = 0.5f;
    public float vignetteCenterY = 0.5f;
    public float vignetteIntensity = 0f;
    public float vignetteSmoothness = 0.2f;

    [Header("Panini Projection")]
    public float paniniDistance = 0f;
    public float paniniCropToFit = 1f;

    [Header("Depth of Field - Gaussian")]
    public float dofGaussianStart = 10f;
    public float dofGaussianEnd = 30f;

    [Header("Depth of Field - Bokeh")]
    public float dofFocusDistance = 10f;
    public float dofFocalLength = 50f;
    public float dofAperture = 5.6f;

    [Header("Motion Blur")]
    public float motionBlurIntensity = 0f;
    public float motionBlurClamp = 0.05f;

    [Header("Film Grain")]
    public float filmGrainIntensity = 0f;
    public float filmGrainResponse = 0.8f;

    [Header("Lens Flare (Screen Space)")]
    public float lensFlareIntensity = 0f;
    public Color lensFlareTintColor = Color.white;
    public int lensFlareSamples = 1;
    public float lensFlareVignetteEffect = 1f;
    public float lensFlareStartingPosition = 1.25f;
    public float lensFlareScale = 1.5f;
    public float lensFlareStreaksIntensity = 0f;
    public float lensFlareStreaksLength = 0.5f;
    public float lensFlareStreaksOrientation = 0f;
    public float lensFlareStreaksThreshold = 0.25f;
    public float lensFlareChromaIntensity = 0.5f;

    [Header("Lift Gamma Gain")]
    public Vector4 lift = new Vector4(1f, 1f, 1f, 0f);
    public Vector4 gamma = new Vector4(1f, 1f, 1f, 0f);
    public Vector4 gain = new Vector4(1f, 1f, 1f, 0f);

    [Header("Channel Mixer")]
    public float mixerRedR = 100f;
    public float mixerRedG = 0f;
    public float mixerRedB = 0f;
    public float mixerGreenR = 0f;
    public float mixerGreenG = 100f;
    public float mixerGreenB = 0f;
    public float mixerBlueR = 0f;
    public float mixerBlueG = 0f;
    public float mixerBlueB = 100f;

    [Header("Volume Weight")]
    public float volumeWeight = 1f;

    private Bloom _bloom;
    private ColorAdjustments _colorAdj;
    private WhiteBalance _whiteBalance;
    private SplitToning _splitToning;
    private LensDistortion _lensDistortion;
    private ChromaticAberration _chroma;
    private Vignette _vignette;
    private PaniniProjection _panini;
    private DepthOfField _dof;
    private MotionBlur _motionBlur;
    private FilmGrain _filmGrain;
    private ScreenSpaceLensFlare _lensFlare;
    private LiftGammaGain _liftGammaGain;
    private ChannelMixer _channelMixer;

    private void Awake()
    {
        if (_volume == null) _volume = GetComponent<Volume>();
        FetchComponents();
    }

    private void OnValidate()
    {
        if (_volume == null) _volume = GetComponent<Volume>();
        FetchComponents();
    }

    private void FetchComponents()
    {
        if (_volume == null || _volume.profile == null) return;
        _volume.profile.TryGet(out _bloom);
        _volume.profile.TryGet(out _colorAdj);
        _volume.profile.TryGet(out _whiteBalance);
        _volume.profile.TryGet(out _splitToning);
        _volume.profile.TryGet(out _lensDistortion);
        _volume.profile.TryGet(out _chroma);
        _volume.profile.TryGet(out _vignette);
        _volume.profile.TryGet(out _panini);
        _volume.profile.TryGet(out _dof);
        _volume.profile.TryGet(out _motionBlur);
        _volume.profile.TryGet(out _filmGrain);
        _volume.profile.TryGet(out _lensFlare);
        _volume.profile.TryGet(out _liftGammaGain);
        _volume.profile.TryGet(out _channelMixer);
    }

    private void LateUpdate()
    {
        if (_volume == null) return;
        if (_bloom == null || _lensFlare == null) FetchComponents();
        _volume.weight = volumeWeight;

        SyncBloom();
        SyncColorAdj();
        SyncWhiteBalance();
        SyncSplitToning();
        SyncLensDistortion();
        SyncChroma();
        SyncVignette();
        SyncPanini();
        SyncDoF();
        SyncMotionBlur();
        SyncFilmGrain();
        SyncLensFlare();
        SyncLiftGammaGain();
        SyncChannelMixer();
    }

    private void SyncBloom()
    {
        if (_bloom == null) return;
        _bloom.threshold.value = bloomThreshold;
        _bloom.intensity.value = bloomIntensity;
        _bloom.scatter.value = bloomScatter;
        _bloom.tint.value = bloomTint;
        _bloom.dirtIntensity.value = bloomDirtIntensity;
    }

    private void SyncColorAdj()
    {
        if (_colorAdj == null) return;
        _colorAdj.postExposure.value = postExposure;
        _colorAdj.contrast.value = contrast;
        _colorAdj.colorFilter.value = colorFilter;
        _colorAdj.hueShift.value = hueShift;
        _colorAdj.saturation.value = saturation;
    }

    private void SyncWhiteBalance()
    {
        if (_whiteBalance == null) return;
        _whiteBalance.temperature.value = whiteTemperature;
        _whiteBalance.tint.value = whiteTint;
    }

    private void SyncSplitToning()
    {
        if (_splitToning == null) return;
        _splitToning.shadows.value = splitShadows;
        _splitToning.highlights.value = splitHighlights;
        _splitToning.balance.value = splitBalance;
    }

    private void SyncLensDistortion()
    {
        if (_lensDistortion == null) return;
        _lensDistortion.intensity.value = lensIntensity;
        _lensDistortion.xMultiplier.value = lensXMultiplier;
        _lensDistortion.yMultiplier.value = lensYMultiplier;
        _lensDistortion.center.value = new Vector2(lensCenterX, lensCenterY);
        _lensDistortion.scale.value = lensScale;
    }

    private void SyncChroma()
    {
        if (_chroma == null) return;
        _chroma.intensity.value = chromaIntensity;
    }

    private void SyncVignette()
    {
        if (_vignette == null) return;
        _vignette.color.value = vignetteColor;
        _vignette.center.value = new Vector2(vignetteCenterX, vignetteCenterY);
        _vignette.intensity.value = vignetteIntensity;
        _vignette.smoothness.value = vignetteSmoothness;
    }

    private void SyncPanini()
    {
        if (_panini == null) return;
        _panini.distance.value = paniniDistance;
        _panini.cropToFit.value = paniniCropToFit;
    }

    private void SyncDoF()
    {
        if (_dof == null) return;
        _dof.gaussianStart.value = dofGaussianStart;
        _dof.gaussianEnd.value = dofGaussianEnd;
        _dof.focusDistance.value = dofFocusDistance;
        _dof.focalLength.value = dofFocalLength;
        _dof.aperture.value = dofAperture;
    }

    private void SyncMotionBlur()
    {
        if (_motionBlur == null) return;
        _motionBlur.intensity.value = motionBlurIntensity;
        _motionBlur.clamp.value = motionBlurClamp;
    }

    private void SyncFilmGrain()
    {
        if (_filmGrain == null) return;
        _filmGrain.intensity.value = filmGrainIntensity;
        _filmGrain.response.value = filmGrainResponse;
    }

    private void SyncLensFlare()
    {
        if (_lensFlare == null) return;
        _lensFlare.intensity.value = lensFlareIntensity;
        _lensFlare.tintColor.value = lensFlareTintColor;
        _lensFlare.samples.value = lensFlareSamples;
        _lensFlare.vignetteEffect.value = lensFlareVignetteEffect;
        _lensFlare.startingPosition.value = lensFlareStartingPosition;
        _lensFlare.scale.value = lensFlareScale;
        _lensFlare.streaksIntensity.value = lensFlareStreaksIntensity;
        _lensFlare.streaksLength.value = lensFlareStreaksLength;
        _lensFlare.streaksOrientation.value = lensFlareStreaksOrientation;
        _lensFlare.streaksThreshold.value = lensFlareStreaksThreshold;
        _lensFlare.chromaticAbberationIntensity.value = lensFlareChromaIntensity;
    }

    private void SyncLiftGammaGain()
    {
        if (_liftGammaGain == null) return;
        _liftGammaGain.lift.value = lift;
        _liftGammaGain.gamma.value = gamma;
        _liftGammaGain.gain.value = gain;
    }

    private void SyncChannelMixer()
    {
        if (_channelMixer == null) return;
        _channelMixer.redOutRedIn.value = mixerRedR;
        _channelMixer.redOutGreenIn.value = mixerRedG;
        _channelMixer.redOutBlueIn.value = mixerRedB;
        _channelMixer.greenOutRedIn.value = mixerGreenR;
        _channelMixer.greenOutGreenIn.value = mixerGreenG;
        _channelMixer.greenOutBlueIn.value = mixerGreenB;
        _channelMixer.blueOutRedIn.value = mixerBlueR;
        _channelMixer.blueOutGreenIn.value = mixerBlueG;
        _channelMixer.blueOutBlueIn.value = mixerBlueB;
    }
}

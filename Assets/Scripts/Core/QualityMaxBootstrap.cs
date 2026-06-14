using UnityEngine;

public static class QualityMaxBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyMaximumQuality()
    {
        int maxQualityIndex = QualitySettings.names.Length - 1;
        if (maxQualityIndex >= 0)
        {
            QualitySettings.SetQualityLevel(maxQualityIndex, true);
        }

        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.antiAliasing = 8;
        QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 8);
        QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 2f);
        QualitySettings.maximumLODLevel = 0;

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 120f);
        QualitySettings.softParticles = true;
        QualitySettings.realtimeReflectionProbes = true;

        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 1;
    }
}

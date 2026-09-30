using UnityEditor;

public sealed class BrandArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Brand/")) return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;
        importer.sRGBTexture=true;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.isReadable=assetPath.EndsWith("FordPerformance.jpg");
        importer.mipmapEnabled=true; importer.filterMode=UnityEngine.FilterMode.Bilinear;
        importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=2048;
    }
}

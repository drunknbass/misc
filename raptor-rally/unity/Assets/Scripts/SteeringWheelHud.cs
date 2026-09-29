using UnityEngine;

namespace RaptorRally
{
    // Reference-guided wheel art, rotated in the same coordinate space as the HUD.
    public sealed class SteeringWheelHud
    {
        readonly Texture2D texture;
        readonly bool ownsTexture;
        public SteeringWheelHud()
        {
            var source=Resources.Load<Texture2D>("HUD/RaptorSteeringWheel");
            if(source==null) { Debug.LogError("Missing Raptor steering wheel HUD asset."); return; }
            texture=source;
            if(QualitySettings.activeColorSpace==ColorSpace.Linear)
            {
                // Match the HUD's existing linear-color treatment. IMGUI otherwise
                // displays this dark leather asset as washed-out silver.
                var pixels=source.GetPixels();
                for(int i=0;i<pixels.Length;i++) pixels[i]=pixels[i].linear;
                texture=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false) { name="Wheel IMGUI color corrected",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
                texture.SetPixels(pixels); texture.Apply(false,true); ownsTexture=true;
            }
        }

        public void Draw(float angle,bool compact)
        {
            if(texture==null) return;
            Vector2 center=new Vector2(1470,compact?763:747);
            float diameter=compact?174:206;
            Matrix4x4 saved=GUI.matrix;
            GUI.matrix=RotationMatrix(saved,center,angle);
            GUI.DrawTexture(new Rect(center.x-diameter/2,center.y-diameter/2,diameter,diameter),texture);
            GUI.matrix=saved;
        }

        public static Matrix4x4 RotationMatrix(Matrix4x4 canvas,Vector2 center,float angle)
        {
            // Compose in HUD coordinates before the canvas scale/letterbox offset.
            // RotateAroundPivot mixes those spaces when GUI.matrix is already scaled.
            Vector3 pivot=new Vector3(center.x,center.y,0);
            return canvas*Matrix4x4.Translate(pivot)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Translate(-pivot);
        }

        public void Dispose()
        {
            if(!ownsTexture) return;
            if(Application.isPlaying) Object.Destroy(texture); else Object.DestroyImmediate(texture);
        }

    }
}

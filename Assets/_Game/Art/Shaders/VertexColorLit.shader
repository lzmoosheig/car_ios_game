Shader "Overhaul/VertexColorLit"
{
    // Some Kenney packs (racing-kit, nature-kit) ship geometry with baked vertex
    // colors instead of a UV atlas. Built-in RP's Standard shader ignores vertex
    // color, so those meshes render flat white without this. Simple Lambert
    // surface shader that reads COLOR into albedo. Doc 05 art direction: readable
    // color-coded low-poly, no need for PBR maps here.
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert

        struct Input
        {
            float4 vertColor;
        };

        fixed4 _Tint;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vertColor = v.color;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.vertColor.rgb * _Tint.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

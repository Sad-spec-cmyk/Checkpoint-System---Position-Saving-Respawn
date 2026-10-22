Shader "Checkpoint System/Industrial Surface"
{
    Properties
    {
        _MainTex ("Surface Albedo", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Scale ("World Tiling", Float) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _Metallic, _Glossiness;
        float _Scale;
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 weights = pow(abs(IN.worldNormal), 4);
            weights /= max(weights.x + weights.y + weights.z, 0.0001);
            float3 p = IN.worldPos * _Scale;
            fixed3 albedo = tex2D(_MainTex, p.yz).rgb * weights.x
                + tex2D(_MainTex, p.xz).rgb * weights.y
                + tex2D(_MainTex, p.xy).rgb * weights.z;
            o.Albedo = albedo * _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness * (0.75 + albedo.r * 0.5);
            o.Occlusion = 0.9 + albedo.r * 0.1;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}

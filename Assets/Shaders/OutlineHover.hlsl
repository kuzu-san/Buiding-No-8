Shader("Custom/OutlineHover")
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 0, 1)
        _OutlineWidth ("Outline Width", Float) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent+1" }

        Pass
        {
            Name("OUTLINE")
            
            CullFront; // Рендерим только внутреннюю сторону (инвертированные нормали)
            ZWriteOff;
            ZTestLEqual;

            CGPROGRAM;
            #pragma vertex vert
            #pragma fragment frag
#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
};

struct v2f
{
    float4 pos : SV_POSITION;
};

float4 _OutlineColor;
float _OutlineWidth;

v2f vert(appdata v)
{
    v2f o;
                // Смещаем вершины вдоль нормалей → создаём обводку
    float3 norm = normalize(v.normal);
    float4 pos = v.vertex + float4(norm * _OutlineWidth, 0);
    o.pos = UnityObjectToClipPos(pos);
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    return _OutlineColor;
}
            ENDCG
        }
    }
}
shader_type canvas_item;
uniform float strength = 0.004;
void fragment() {
    vec2 uv = UV;
    uv.x += sin(UV.y * 60.0 + TIME * 1.7) * strength;
    vec4 c = texture(TEXTURE, uv);
    float glint = step(0.94, sin(UV.x * 90.0 + UV.y * 42.0 + TIME * 1.4));
    COLOR = vec4(c.rgb + vec3(0.07,0.1,0.1) * glint, c.a);
}

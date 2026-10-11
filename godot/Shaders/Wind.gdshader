shader_type canvas_item;
uniform float strength = 0.8;
void vertex() {
    VERTEX.x += sin(TIME * 1.3 + MODEL_MATRIX[3].x * 0.02) * max(0.0, -VERTEX.y) / 120.0 * strength;
}

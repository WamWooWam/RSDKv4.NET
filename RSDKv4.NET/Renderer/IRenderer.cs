using Microsoft.Xna.Framework.Graphics;

namespace RSDKv4.Render;

public interface IRenderer
{
    void Draw();
    void Reset();

    void SetScreenDimensions(int width, int height);

    void BeginDraw();
    void EndDraw();

    void Copy16x16Tile(int dest, int src);

    void EnsureBlendMode(BlendMode mode);

    void ClearScreen(byte clearColour);
    void Draw3DFloorLayer(int layer);
    void Draw3DSkyLayer(int layer);
    void DrawAdditiveBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int alpha, int surfaceNum);
    void DrawAlphaBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int alpha, int surfaceNum);
    void DrawBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    void DrawFadedQuad(Quad2D face, uint colour, uint fogColour, int alpha);
    void DrawHLineScrollLayer(byte layerNum);
    void DrawQuad(Quad2D face, int rgbVal);
    void DrawRectangle(int xPos, int yPos, int xSize, int ySize, int r, int g, int b, int alpha);
    void DrawRotatedSprite(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xBegin, int yBegin, int xSize, int ySize, int rotAngle, int surfaceNum);
    void DrawRotoZoomSprite(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xBegin, int yBegin, int xSize, int ySize, int rotAngle, int scale, int surfaceNum);
    void DrawScaledChar(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xScale, int yScale, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    void DrawScaledSprite(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xScale, int yScale, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    void DrawScaledTintMask(byte direction, int xPos, int yPos, int xPivot, int yPivot, int xScale, int yScale, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    void DrawSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int surfaceNum);
    void DrawSpriteFlipped(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int direction, int surfaceNum);
    void DrawSubtractiveBlendedSprite(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int alpha, int surfaceNum);
    void DrawTexturedBlendedQuad(Quad2D face, int surfaceNum);
    void DrawTexturedQuad(Quad2D face, int surfaceNum);
    void DrawTintRectangle(int xPos, int yPos, int xSize, int ySize);
    void DrawTintSpriteMask(int xPos, int yPos, int xSize, int ySize, int xBegin, int yBegin, int tableNo, int surfaceNum);
    void DrawVLineScrollLayer(int layer);

    Texture2D CopyRetroBuffer();
}

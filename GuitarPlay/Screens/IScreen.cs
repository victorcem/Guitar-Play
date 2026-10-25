using Microsoft.Xna.Framework;

namespace GuitarPlay.Screens;

public interface IScreen
{
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}

using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Graphics;
using SonicOrca.Geometry;

namespace SonicOrca.Funkin.Meta.State
{
    internal sealed class PlayState : IGameState
    {
        private readonly FunkinGameContext _context;

        public PlayState(FunkinGameContext context)
        {
            _context = context;
        }

        public IEnumerable<UpdateResult> Update()
        {
            while (true)
                yield return UpdateResult.Next();
        }

        public void Draw()
        {
            I2dRenderer r2d = _context.Renderer.Get2dRenderer();
            r2d.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
            r2d.BlendMode = BlendMode.Opaque;
            r2d.Colour = new Colour(0.12, 0.14, 0.22, 1.0);
            r2d.RenderQuad(r2d.Colour, r2d.ClipRectangle);
        }

        public void Dispose()
        {
        }
    }
}
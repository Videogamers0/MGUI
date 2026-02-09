using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Rendering
{
    public enum RasterizerType
    {
        Default,
        SolidScissorTest,
        Solid,
        WireframeScissorTest,
        Wireframe
    }

    public enum BlendType
    {
        /// <summary><see cref="BlendState.AlphaBlend"/></summary>
        Default,
        /// <summary><see cref="BlendState.Additive"/></summary>
        Additive,
        /// <summary><see cref="BlendState.AlphaBlend"/></summary>
        AlphaBlend,
        /// <summary><see cref="BlendState.NonPremultiplied"/></summary>
        NonPremultiplied,
        /// <summary><see cref="BlendState.Opaque"/></summary>
        Opaque
    }

    public enum SamplerType
    {
        /// <summary><see cref="SamplerState.LinearClamp"/></summary>
        Default,
        /// <summary><see cref="SamplerState.AnisotropicClamp"/></summary>
        AnisotropicClamp,
        /// <summary><see cref="SamplerState.AnisotropicWrap"/></summary>
        AnisotropicWrap,
        /// <summary><see cref="SamplerState.LinearClamp"/></summary>
        LinearClamp,
        /// <summary><see cref="SamplerState.LinearWrap"/></summary>
        LinearWrap,
        /// <summary><see cref="SamplerState.PointClamp"/></summary>
        PointClamp,
        /// <summary><see cref="SamplerState.PointWrap"/></summary>
        PointWrap
    }

    public enum DepthStencilType
    {
        /// <summary><see cref="DepthStencilState.None"/></summary>
        Default,
        /// <summary><see cref="DepthStencilState.DepthRead"/></summary>
        DepthRead,
        /// <summary><see cref="DepthStencilState.None"/></summary>
        None
    }

    /// <summary>Stores settings used by <see cref="SpriteBatch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, Effect, Matrix?)"/></summary>
    public record class DrawSettings(Matrix Transform, RasterizerType RasterizerType = RasterizerType.SolidScissorTest, SpriteSortMode Sort = SpriteSortMode.Deferred,
        BlendType BlendType = BlendType.AlphaBlend, SamplerType SamplerType = SamplerType.PointClamp, DepthStencilType DepthStencilType = DepthStencilType.None)
    {
        private Matrix? _InverseTransform;
        public Matrix InverseTransform
        {
            get
            {
                if (!_InverseTransform.HasValue)
                {
                    _InverseTransform = IsIdentityTransform ? Matrix.Identity : Matrix.Invert(Transform);
                }
                return _InverseTransform.Value;
            }
        }

        public bool IsIdentityTransform { get; } = Transform == Matrix.Identity;

        public static DrawSettings Default => new(Matrix.Identity);

        private static readonly Dictionary<RasterizerType, RasterizerState> RasterizerMap = new()
        {
            { RasterizerType.Default, new() { CullMode = CullMode.None } },
            { RasterizerType.SolidScissorTest, new() { FillMode = FillMode.Solid, ScissorTestEnable = true, CullMode = CullMode.None } },
            { RasterizerType.Solid, new() { FillMode = FillMode.Solid, ScissorTestEnable = false, CullMode = CullMode.None } },
            { RasterizerType.WireframeScissorTest, new() { FillMode = FillMode.WireFrame, ScissorTestEnable = true, CullMode = CullMode.None } },
            { RasterizerType.Wireframe, new() { FillMode = FillMode.WireFrame, ScissorTestEnable = false, CullMode = CullMode.None } }
        };

        private static readonly Dictionary<BlendType, BlendState> BlendMap = new()
        {
            { BlendType.Default, BlendState.AlphaBlend },
            { BlendType.Additive, BlendState.Additive },
            { BlendType.AlphaBlend, BlendState.AlphaBlend },
            { BlendType.NonPremultiplied, BlendState.NonPremultiplied },
            { BlendType.Opaque, BlendState.Opaque }
        };

        private static readonly Dictionary<SamplerType, SamplerState> SamplerMap = new()
        {
            { SamplerType.Default, SamplerState.LinearClamp },
            { SamplerType.AnisotropicClamp, SamplerState.AnisotropicClamp },
            { SamplerType.AnisotropicWrap, SamplerState.AnisotropicWrap },
            { SamplerType.LinearClamp, SamplerState.LinearClamp },
            { SamplerType.LinearWrap, SamplerState.LinearWrap },
            { SamplerType.PointClamp, SamplerState.PointClamp },
            { SamplerType.PointWrap, SamplerState.PointWrap },
        };

        private static readonly Dictionary<DepthStencilType, DepthStencilState> DepthStencilMap = new()
        {
            { DepthStencilType.Default, DepthStencilState.None },
            { DepthStencilType.DepthRead, DepthStencilState.DepthRead },
            { DepthStencilType.None, DepthStencilState.None }
        };

        public RasterizerState RasterizerState => RasterizerMap[RasterizerType];
        public BlendState BlendState => BlendMap[BlendType];
        public SamplerState SamplerState => SamplerMap[SamplerType];
        public DepthStencilState DepthStencilState => DepthStencilMap[DepthStencilType];

        public void BeginDraw(SpriteBatch SB) => SB.Begin(Sort, BlendState, SamplerState, DepthStencilState, RasterizerState, null, Transform);
    }
}

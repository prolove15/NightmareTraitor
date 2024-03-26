using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMobileInput
{
    public class RaycastZone : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        protected override void OnPopulateMesh(Mesh m)
        {
            m.Clear();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiltyCharacter
{
    public interface IRagdollBase
    {
        void ResetRagdoll();
        void EnableRagdoll();
        void RagdollGetUp();
        bool ragdolled { get; }
    }
}

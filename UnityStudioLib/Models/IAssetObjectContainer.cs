using System.Collections.Generic;

namespace UnityStudio.Models {
    internal interface IAssetObjectContainer : IReadOnlyDictionary<string, object> {

        IReadOnlyDictionary<string, object> Variables { get; }

    }
}

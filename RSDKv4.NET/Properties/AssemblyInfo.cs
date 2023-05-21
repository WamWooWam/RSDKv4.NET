using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if SILVERLIGHT
[assembly:CodeGeneration(CodeGenerationFlags.EnableFPIntrinsicsUsingSIMD)]
#endif

#if XNA
[assembly:Guid("97A28523-B9D5-442A-9EEB-FE7CD9679C45")]
#endif
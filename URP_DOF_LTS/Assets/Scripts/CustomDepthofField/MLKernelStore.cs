using UnityEngine;

public static class MLKernelStore
{
    public static Vector4[] Kernel;
    public static int KernelCount => Kernel != null ? Kernel.Length : 0;
}
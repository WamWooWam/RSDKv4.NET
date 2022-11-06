# Precompiled Shaders

For simplicity, this folder contains precompiled shaders for use in the game, each folder contains shaders for it's corresponding target.

If you need to compile them yourself, here's how:

### FNA

You will need the DirectX Effect Compiler (`fxc`)

For each shader `.fx` file, run the following command:

```cmd
> fxc /T fx_2_0 /Fo <output>.fxb <input>.fx
```

### XNA

You will need XNA Game Studio.

- Open the `ShaderCompilerProject.sln` file under `XNA\ShaderCompilerProject` in Visual Studio
- Build the project
- Copy the contents from `bin\x86\Debug\Content` into your project.

### MonoGame

TODO.
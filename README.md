# MonoGame.Templates

MonoGame repo to hold our project/item templates.

## Project Templates

> [!NOTE]
>
> When creating your projects from these templates, use commands like `dotnet new mg2dstartkit -n {SomeName}`.

| Name | Short Code | Description |
| - | - | - |
| 2D StartKit | mg2dstartkit | A solution with a shared "Core" library and "Project" definitions for DesktopGL, iOS, and Android. |
| Blank 2D StartKit | mgblank2dstartkit | A complete solution, complete with content, with a shared "Core" library and "Project" definitions for DesktopGL, iOS, and Android. |
| Android | mgandroid | A blank Android MonoGame project |
| Cross-Platform desktop | mgdesktopgl | A blank Desktop GL MonoGame project |
| iOS | mgios | A blank iOS MonoGame project |
| Windows DirectX11 | mgwindowsdx | A blank Windows DX11 MonoGame project |
| Game Library | mglib | A blank MonoGame class library project, including MG references. |
| Content Builder Project | mgcb | A template Content Builder project, with default Builder script and Assets folder. |
| Content Pipeline Extension | mgpipeline | A template Content Pipeline Extension project. |

> [!Note]
> Legacy Templates
>
> - Shared Library - mgshared - Legacy Xamarin template.

## Preview Project Templates

| Name | Short Code | Description |
| - | - | - |
| Content Builder StartKit | mg2dmgcbstartkit | Demo project complete with solution and content. Includes new VK/DX12 targets. |
| Content Builder Blank StartKit | mgblankmgcbstartkit | Demo solution without content. Includes new VK/DX12 targets. |

## Rules for Project naming

You **MUST** follow the standard .NET conventions when naming projects:

- Do NOT use numbers as the 1st character of the name of your project. These will be replaced by underscores, `_`, and will break the templates.
- Do NOT begin with a special character
- Spaces are not allowed; they will be replaced with underscores, `_`, which will break the template.

## Item Templates

> [!NOTE]
>
> When creating your items from these templates, use commands like `dotnet new mgsf -n {SomeName}`.

| Name | Short Code | Description |
| - | - | - |
| Sprite Font template | mgsf | SpriteFont item template for Content Projects |
| Content Pipeline Extension | mgpipelineitem | A template Content Pipeline Extension Item set (importer/processor) |

> [!TIP]
>
> The SpriteFont template also includes a default Font file, to include it when creating an item, use the `--IncludeFont` argument.
>
> `dotnet new mgsf -n {SomeName} --IncludeFont`
>
> *Note this will error if the font file already exists in the directory.

> [!TIP]
> We recommend when using the "Content Pipeline Extension" Item template, to also use the `-o` argument to place it in its own folder.  Not mandatory.
>
> E.G.
> `dotnet new mgpipelineitem -n {SomeName} -o {SomeFolderName}`

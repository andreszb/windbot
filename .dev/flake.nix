# Local dev environment for WindBot on NixOS. NOT part of upstream:
# this whole .dev/ dir, .envrc and CLAUDE.md are listed in .git/info/exclude.
# Loaded via `.envrc` → `use flake path:.dev` (path: so git never needs to track it).
{
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";

  outputs = {nixpkgs, ...}: let
    system = "x86_64-linux";
    pkgs = nixpkgs.legacyPackages.${system};

    # MDPro4's card DB; WindBot needs it to resolve card IDs.
    cdb = "$HOME/Games/MDPro4-1.0.2/Data/locales/en-US/cards.cdb";

    root = ''root=$(git rev-parse --show-toplevel) && cd "$root"'';

    wb-build = pkgs.writeShellScriptBin "wb-build" ''
      ${root}
      msbuild WindBot.sln /p:Configuration=Release /v:minimal /nologo "$@"
      ln -sf "${cdb}" bin/Release/cards.cdb
    '';

    # wb-run Deck=Toon Host=127.0.0.1 Port=7911 [Hand=1..3] ...
    wb-run = pkgs.writeShellScriptBin "wb-run" ''
      ${root}/bin/Release
      exec mono WindBot.exe Chat=False Debug=True "$@"
    '';
  in {
    devShells.${system}.default = pkgs.mkShell {
      packages = [pkgs.mono pkgs.msbuild pkgs.sqlite wb-build wb-run];
      # Mono.Data.Sqlite dlopens libsqlite3.so.0; NixOS has no global lib path.
      LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [pkgs.sqlite];
    };
  };
}

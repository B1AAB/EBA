# Website

This website is built using [Docusaurus](https://docusaurus.io/), a modern static website generator.

### Installation

```
$ yarn
```

### Local Development

```
$ yarn start
```

This command starts a local development server and opens up a browser window. Most changes are reflected live without having to restart the server.

### Build

```
$ yarn build
```

This command generates static content into the `build` directory and can be served using any static contents hosting service.

### Update the hero graph

The landing page's graph is a prebuilt from
panorama in `static/graph-viz/`.
To rebuild it from the latest panorama, run from `website/`:

- Linux/macOS:

    ```
    export PANORAMA_DIR=/path/to/panorama
    git -C "$PANORAMA_DIR" pull
    npm --prefix "$PANORAMA_DIR" install
    npm install
    node graph-viz/build.mjs
    ```

- PowerShell:

    ```
    $env:PANORAMA_DIR = "C:\path\to\panorama"
    git -C $env:PANORAMA_DIR pull
    npm --prefix $env:PANORAMA_DIR install
    npm install
    node graph-viz/build.mjs
    ```

Commit `static/graph-viz/` and `src/data/graphViz.js`.

### Deployment

Using SSH:

```
$ USE_SSH=true yarn deploy
```

Not using SSH:

```
$ GIT_USER=<Your GitHub username> yarn deploy
```

If you are using GitHub pages for hosting, this command is a convenient way to build the website and push to the `gh-pages` branch.

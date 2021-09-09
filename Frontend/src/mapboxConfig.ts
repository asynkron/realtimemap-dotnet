// get a token from https://www.mapbox.com/, more info in readme.md
const mapboxAccessToken = "<your access token>";

// in order prevent commiting your token by accident, I strongly recommend setting skip-worktree flag on a file:
// git update-index --skip-worktree Frontend/src/mapboxConfig.ts
// this can be reversed with:
// git update-index --no-skip-worktree Frontend/src/mapboxConfig.ts

export default {

  getAccessToken(): string {
    if (mapboxAccessToken as string === "<your access token>") {
      alert("Mapbox access token not set. Please set it in mapboxConfig.ts!")
      throw new Error("Mapbox access token not set.");
    }

    return mapboxAccessToken;
  }

}

// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

namespace RhythmVisualizatorPro.FileBrowser.Scripts.UI {

    public class SetupFileBrowserUI: FileBrowserUI {

        protected override void SetupParents() {
            // Find directories parent to group directory buttons
            DirectoriesParent = Utilities.FindGameObjectOrError("Directories");
            // Find files parent to group file buttons
            FilesParent = Utilities.FindGameObjectOrError("Files");
        }
    }
}


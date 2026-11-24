
import { useState } from "react";

export default function ProjectSelector({
    projectTitle,
    loadProject,
    isLoading,
}) {
    const [isSelecting, setIsSelecting] = useState(false);
    const [error, setError] = useState(null);

    async function handleSelectProject() {
        if (!window.showDirectoryPicker) {
            setError("Folder selection requires Chrome or Edge.");
            return;
        }

        setIsSelecting(true);
        setError(null);

        try {
            // Let the user select a project folder.
            const directory = await window.showDirectoryPicker({
                id: "knowtable-project",
                mode: "read",
            });

            // Read the project manifest.
            const manifestHandle = await directory.getFileHandle(
                "project.json"
            );

            const file = await manifestHandle.getFile();
            const project = JSON.parse(await file.text());

            if (!project.id) {
                throw new Error(
                    "The selected project.json has no project ID."
                );
            }

            // Load the project through the existing API workflow.
            await loadProject(project.id);

        } catch (err) {
            if (err.name === "AbortError") return;

            if (err.name === "NotFoundError") {
                setError("No project.json found in that folder.");
            } else {
                setError(err.message || "Failed to select project.");
            }
        } finally {
            setIsSelecting(false);
        }
    }

    return (
        <div style={{
            position: "fixed",
            top: 16,
            left: 150,
            zIndex: 10000,
        }}>
            <button
                type="button"
                onClick={handleSelectProject}
                disabled={isLoading || isSelecting}
                style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 12,
                    padding: "8px 12px",
                    background: "white",
                    border: "1px solid #ccc",
                    borderRadius: 5,
                    cursor: isSelecting ? "wait" : "pointer",
                }}
            >
                <span>
                    {isSelecting
                        ? "Opening project..."
                        : `Current project: ${projectTitle || "Unsaved project"}`}
                </span>

                <span aria-hidden="true">▾</span>
            </button>

            {error && (
                <p role="alert" style={{ color: "red" }}>
                    {error}
                </p>
            )}
        </div>
    );
}

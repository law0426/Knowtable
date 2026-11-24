
import { useState } from "react";
import useProjectService from "../hooks/useProjectService";

export default function ProjectSaveButton({
    projectId,
    projectTitle,
    projectTags,
    cards,
    isDirty,
    isProjectDetailsDirty,
    activateSavedProject,
}) {
    const { saveProject, isSaving, error } = useProjectService({
        projectId,
        projectTitle,
        projectTags,
        isDirty,
        isProjectDetailsDirty,
        activateSavedProject,
    });

    const [isNaming, setIsNaming] = useState(false);
    const [draftTitle, setDraftTitle] = useState("");
    const [nameError, setNameError] = useState(null);

    function handleSaveClick() {
        if (!projectId) {
            setDraftTitle(projectTitle ?? "");
            setNameError(null);
            setIsNaming(true);
            return;
        }

        saveProject(cards);
    }

    async function handleCreateProject(event) {
        event.preventDefault();

        const title = draftTitle.trim();

        if (!title) {
            setNameError("Please enter a project title.");
            return;
        }

        try {
            // Check existing project titles.
            const response = await fetch("/api/projects");

            if (!response.ok) {
                throw new Error(
                    `Loading projects failed (${response.status}).`
                );
            }

            const projects = await response.json();

            const duplicate = projects.some(
                project =>
                    (project.title ?? "").trim().toLowerCase() ===
                    title.toLowerCase()
            );

            if (duplicate) {
                setNameError(
                    "A project with this title already exists."
                );
                return;
            }

            setNameError(null);

            await saveProject(cards, title);

            setIsNaming(false);

        } catch (err) {
            setNameError(err.message);
        }
    }

    const saveDisabled =
        isSaving ||
        (projectId && !isDirty && !isProjectDetailsDirty);

    return (
        <>
            <div style={{
                position: "fixed",
                top: 16,
                right: 16,
                zIndex: 10000,
            }}>
                <button
                    type="button"
                    onClick={handleSaveClick}
                    disabled={saveDisabled}
                    style={{
                        padding: "10px 18px",
                        cursor: isSaving ? "wait" : "pointer",
                    }}
                >
                    {isSaving ? "Saving..." : "Save project"}
                </button>

                {error && (
                    <p role="alert">
                        Save failed: {error}
                    </p>
                )}
            </div>

            {isNaming && (
                <div style={{
                    position: "fixed",
                    inset: 0,
                    background: "rgba(0, 0, 0, 0.4)",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    zIndex: 20000,
                }}>
                    <form
                        onSubmit={handleCreateProject}
                        style={{
                            background: "white",
                            padding: 24,
                            borderRadius: 8,
                            width: 320,
                            display: "flex",
                            flexDirection: "column",
                            gap: 12,
                        }}
                    >
                        <h3 style={{ margin: 0 }}>
                            Name your project
                        </h3>

                        <input
                            autoFocus
                            type="text"
                            value={draftTitle}
                            onChange={e => setDraftTitle(e.target.value)}
                            placeholder="Project title"
                            maxLength={150}
                            style={{ padding: 9 }}
                        />

                        {nameError && (
                            <p role="alert" style={{ color: "red", margin: 0 }}>
                                {nameError}
                            </p>
                        )}

                        <div style={{
                            display: "flex",
                            justifyContent: "flex-end",
                            gap: 8,
                        }}>
                            <button
                                type="button"
                                onClick={() => setIsNaming(false)}
                            >
                                Cancel
                            </button>

                            <button
                                type="submit"
                                disabled={isSaving}
                            >
                                {isSaving ? "Saving..." : "Create project"}
                            </button>
                        </div>
                    </form>
                </div>
            )}
        </>
    );
}

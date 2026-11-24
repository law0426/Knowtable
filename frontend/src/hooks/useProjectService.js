
import { useCallback, useState } from "react";

export default function useProjectService({
    projectId,
    projectTitle,
    projectTags,
    isDirty,
    isProjectDetailsDirty,
    activateSavedProject,
}) {
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState(null);

    const saveProject = useCallback(async (cards, titleOverride) => {
        const isDraft = !projectId;

        if ((!isDraft && !isDirty && !isProjectDetailsDirty) || isSaving) return;
        const title = (titleOverride ?? projectTitle ?? "").trim();

        if (isDraft && !title) {
            setError("A project title is required.");
            return;
        }

        const endpoint = isDraft
            ? "/api/projects/draft/save"
            : `/api/projects/${projectId}/save`;

        setIsSaving(true);
        setError(null);

        try {
            const response = await fetch(endpoint, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    id: projectId ?? "00000000-0000-0000-0000-000000000000",
                    title,
                    tags: projectTags ?? [],
                    cards: cards.map((card) => ({
                        id: card.id,
                        frontContent: card.frontContent ?? {
                            type: "doc",
                            content: [{ type: "paragraph" }],
                        },
                        tagIds: card.tagIds ?? [],
                        x: card.x,
                        y: card.y,
                        width: card.width ?? 240,
                        height: card.height ?? 180,
                    })),
                }),
            });

            if (!response.ok) {
                throw new Error(`Save failed (${response.status})`);
            }

            const project = await response.json();

            // Synchronize the active project after every successful save.
            activateSavedProject(project);

        } catch (err) {
            setError(err.message);
        } finally {
            setIsSaving(false);
        }
    }, [
        projectId,
        projectTitle,
        projectTags,
        isDirty,
        isProjectDetailsDirty,
        isSaving,
        activateSavedProject,
    ]);

    return { saveProject, isSaving, error };
}

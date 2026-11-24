
import { useCallback, useEffect, useRef, useState } from "react";

const PROJECT_ID_KEY = "knowtable.projectId";

export default function useProjectDataManager(replaceCards) {
    const [projectId, setProjectId] = useState(
        () => localStorage.getItem(PROJECT_ID_KEY)
    );
    const [projectTitle, setProjectTitle] = useState("");
    const [projectTags, setProjectTags] = useState([]);
    const [isProjectDetailsDirty, setIsProjectDetailsDirty] = useState(false);

    const [isLoading, setIsLoading] = useState(true);
    const [isStartingNewProject, setIsStartingNewProject] = useState(false);
    const [error, setError] = useState(null);

    const started = useRef(false);

    const applyProject = useCallback((project) => {
        localStorage.setItem(PROJECT_ID_KEY, project.id);

        setProjectId(project.id);
        setProjectTitle(project.title ?? "");
        setProjectTags(project.tags ?? []);
        setIsProjectDetailsDirty(false);

        replaceCards(project.cards ?? []);
    }, [replaceCards]);

    const fetchDraft = useCallback(async () => {
        const response = await fetch("/api/projects/draft");

        if (!response.ok) {
            throw new Error(`Loading draft failed: ${response.status}`);
        }

        const draft = await response.json();

        return {
            ...draft,
            cards: (draft.cards ?? []).map((card) => ({
                ...card,
                x: window.innerWidth / 2,
                y: window.innerHeight / 2,
            })),
        };
    }, []);

    const startNewProject = useCallback(async () => {
        setIsStartingNewProject(true);
        setError(null);

        try {
            const draft = await fetchDraft();

            localStorage.removeItem(PROJECT_ID_KEY);

            setProjectId(null);
            setProjectTitle("");
            setProjectTags([]);
            setIsProjectDetailsDirty(false);

            replaceCards(draft.cards ?? []);

            return true;
        } catch (err) {
            setError(err.message);
            return false;
        } finally {
            setIsStartingNewProject(false);
        }
    }, [fetchDraft, replaceCards]);

    const loadProject = useCallback(async (id) => {
        setIsLoading(true);
        setError(null);

        try {
            const response = await fetch(`/api/projects/${id}`);

            if (!response.ok) {
                throw new Error(`Loading project failed: ${response.status}`);
            }

            const project = await response.json();

            applyProject(project);

            return true;
        } catch (err) {
            setError(err.message);
            return false;
        } finally {
            setIsLoading(false);
        }
    }, [applyProject]);

    const activateSavedProject = useCallback((project) => {
        applyProject(project);
    }, [applyProject]);

    const updateProjectTitle = useCallback((title) => {
        setProjectTitle(title);
        setIsProjectDetailsDirty(true);
    }, []);

    const updateProjectTags = useCallback((tags) => {
        setProjectTags(tags);
        setIsProjectDetailsDirty(true);
    }, []);

    useEffect(() => {
        if (started.current) return;
        started.current = true;

        async function initializeProject() {
            try {
                const id = localStorage.getItem(PROJECT_ID_KEY);

                if (id) {
                    const response = await fetch(`/api/projects/${id}`);

                    if (response.ok) {
                        const project = await response.json();
                        applyProject(project);
                        return;
                    }

                    if (response.status !== 404) {
                        throw new Error(
                            `Loading project failed: ${response.status}`
                        );
                    }

                    localStorage.removeItem(PROJECT_ID_KEY);
                }

                const draft = await fetchDraft();

                setProjectId(null);
                setProjectTitle("");
                setProjectTags([]);
                setIsProjectDetailsDirty(false);

                replaceCards(draft.cards ?? []);
            } catch (err) {
                setError(err.message);
            } finally {
                setIsLoading(false);
            }
        }

        initializeProject();
    }, [fetchDraft, applyProject, replaceCards]);

    return {
        projectId,
        projectTitle,
        projectTags,
        isProjectDetailsDirty,
        updateProjectTitle,
        updateProjectTags,

        isLoading,
        isStartingNewProject,
        error,

        startNewProject,
        loadProject,
        activateSavedProject,
    };
}

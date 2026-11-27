import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import { ApiError } from '../services/apiClient'
import { createModule, getModule, getModuleOptions, updateModule } from '../services/moduleService'
import type { Module, ModuleOptions } from '../types/module'

/* The icon a new module starts with, the same as ModuleIcons.Default in the API */
const DEFAULT_ICON = 'bi-journal-bookmark'

interface Loaded {
    options: ModuleOptions | null
    module: Module | null
    error: string | null
    done: boolean
}

/* Create module and Edit module, the same pages as Module/Create and Module/Edit in Subapp 1 */
export default function ModuleFormPage() {
    const params = useParams()
    const moduleId = params.id ? Number(params.id) : null

    const [loaded, setLoaded] = useState<Loaded>({ options: null, module: null, error: null, done: false })

    useEffect(() => {
        let active = true

        /* The choices come from the server, so the form always offers exactly what the server accepts */
        Promise.all([getModuleOptions(), moduleId === null ? Promise.resolve(null) : getModule(moduleId)])
            .then(([options, module]) => {
                if (active) setLoaded({ options, module, error: null, done: true })
            })
            .catch((err: unknown) => {
                if (active) {
                    setLoaded({
                        options: null,
                        module: null,
                        error: err instanceof ApiError && err.status === 404 ? 'This module was not found.' : 'The form could not be loaded. Please try again.',
                        done: true,
                    })
                }
            })

        return () => {
            active = false
        }
    }, [moduleId])

    if (!loaded.done) {
        return <LoadingSpinner />
    }

    if (!loaded.options) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                <ErrorAlert message={loaded.error} />
                <Link to="/modules" className="btn btn-outline-primary">Back to modules</Link>
            </div>
        )
    }

    return <ModuleForm key={moduleId ?? 'new'} options={loaded.options} module={loaded.module} />
}

interface ModuleFormProps {
    options: ModuleOptions
    /* null when creating a new module */
    module: Module | null
}

interface FormErrors {
    name?: string
    description?: string
    iconKey?: string
    colorIndex?: string
}

function ModuleForm({ options, module }: ModuleFormProps) {
    const navigate = useNavigate()
    const isEdit = module !== null

    const [name, setName] = useState(module?.name ?? '')
    const [description, setDescription] = useState(module?.description ?? '')
    const [iconKey, setIconKey] = useState(module?.iconKey ?? DEFAULT_ICON)
    const [colorIndex, setColorIndex] = useState<number | null>(module?.colorIndex ?? null)
    const [errors, setErrors] = useState<FormErrors>({})
    const [serverError, setServerError] = useState<string | null>(null)
    const [submitting, setSubmitting] = useState(false)

    /* With no colour chosen yet, the preview uses the first colour, like a new card */
    const previewColor = colorIndex ?? 0

    function validate(): FormErrors {
        const found: FormErrors = {}
        const trimmed = name.trim()

        if (trimmed.length < 2 || trimmed.length > 100) found.name = 'Module name must be between 2 and 100 characters.'
        if (description.trim().length > 500) found.description = 'Description cannot exceed 500 characters.'
        if (!options.icons.some((icon) => icon.key === iconKey)) found.iconKey = 'Choose one of the icons in the list.'

        return found
    }

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setServerError(null)

        const found = validate()
        setErrors(found)
        if (Object.values(found).some(Boolean)) return

        const body = { name: name.trim(), description: description.trim() || null, iconKey, colorIndex }

        setSubmitting(true)
        try {
            if (module) {
                await updateModule(module.id, body)
            } else {
                await createModule(body)
            }
            navigate('/modules', { state: { message: isEdit ? 'Module updated successfully.' : 'Module created successfully.' } })
        } catch (err) {
            if (err instanceof ApiError) {
                /* E.g. "A module with this name already exists." from the server */
                setErrors({
                    name: err.fieldErrors.name?.[0],
                    description: err.fieldErrors.description?.[0],
                    iconKey: err.fieldErrors.iconKey?.[0],
                    colorIndex: err.fieldErrors.colorIndex?.[0],
                })
                setServerError(err.message)
            } else {
                setServerError('The module could not be saved. Please try again.')
            }
            setSubmitting(false)
        }
    }

    return (
        <div className="mx-auto" style={{ maxWidth: 700 }}>
            <h1 className="landing-title mb-2">{isEdit ? 'Edit Module' : 'Create Module'}</h1>
            <p className="lead text-secondary mb-4">{isEdit ? 'Update the module name, description, icon or colour.' : 'Add a new learning area to ITPE3200.'}</p>

            <form onSubmit={handleSubmit} noValidate>
                {serverError && <div className="text-danger mb-3" role="alert">{serverError}</div>}

                <div className="mb-3">
                    <label htmlFor="module-name" className="form-label">Module name</label>
                    <input
                        id="module-name"
                        className={`form-control${errors.name ? ' is-invalid' : ''}`}
                        maxLength={100}
                        value={name}
                        aria-invalid={Boolean(errors.name)}
                        aria-describedby={errors.name ? 'module-name-error' : undefined}
                        onChange={(event) => {
                            setName(event.target.value)
                            setErrors((current) => ({ ...current, name: undefined }))
                        }}
                    />
                    {errors.name && <div id="module-name-error" className="invalid-feedback">{errors.name}</div>}
                </div>

                <div className="mb-3">
                    <label htmlFor="module-description" className="form-label">Description</label>
                    <textarea
                        id="module-description"
                        className={`form-control${errors.description ? ' is-invalid' : ''}`}
                        rows={4}
                        maxLength={500}
                        value={description}
                        aria-invalid={Boolean(errors.description)}
                        aria-describedby={errors.description ? 'module-description-error' : undefined}
                        onChange={(event) => {
                            setDescription(event.target.value)
                            setErrors((current) => ({ ...current, description: undefined }))
                        }}
                    />
                    {errors.description && <div id="module-description-error" className="invalid-feedback">{errors.description}</div>}
                </div>

                {/* The icon choices: radio buttons that look like a grid of icon buttons (icon-option in site.css) */}
                <fieldset className="mb-4">
                    <legend className="form-label fs-6">Icon</legend>
                    <div className="icon-grid">
                        {options.icons.map((icon) => (
                            <label className="icon-option" title={icon.label} key={icon.key}>
                                <input type="radio" name="iconKey" value={icon.key} checked={iconKey === icon.key} aria-label={icon.label} onChange={() => setIconKey(icon.key)} />
                                <span>
                                    <i className={`bi ${icon.key}`} aria-hidden="true"></i>
                                </span>
                            </label>
                        ))}
                    </div>
                    {errors.iconKey && <span className="text-danger small">{errors.iconKey}</span>}

                    {/* Colour choices: round swatches with the eight module colours */}
                    <div className="mt-4" role="radiogroup" aria-labelledby="color-label">
                        <p id="color-label" className="form-label mb-2">Colour</p>
                        <div className="icon-grid">
                            {options.colors.map((color) => (
                                <label className={`icon-option color-option module-color-${color.index}`} title={color.name} key={color.index}>
                                    <input type="radio" name="colorIndex" value={color.index} checked={colorIndex === color.index} aria-label={color.name} onChange={() => setColorIndex(color.index)} />
                                    <span></span>
                                </label>
                            ))}
                        </div>
                        {errors.colorIndex && <span className="text-danger small">{errors.colorIndex}</span>}
                    </div>

                    <div className="mt-3">
                        <p className="small text-secondary mb-2">Preview</p>
                        <div className={`d-flex align-items-center gap-3 border rounded-3 p-3 module-card module-color-${previewColor}`}>
                            <span className="module-icon" aria-hidden="true">
                                <i className={`bi ${iconKey}`}></i>
                            </span>
                            <strong className="text-break">{name.trim() || 'Module name'}</strong>
                        </div>
                    </div>
                </fieldset>

                <div className="d-flex gap-2">
                    <button type="submit" className="btn btn-primary" disabled={submitting}>
                        {submitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>}
                        {isEdit ? 'Save changes' : 'Create module'}
                    </button>
                    <Link to="/modules" className="btn btn-outline-secondary">Cancel</Link>
                </div>
            </form>
        </div>
    )
}
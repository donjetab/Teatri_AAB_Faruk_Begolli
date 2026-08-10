import { EditorContent, useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Underline from '@tiptap/extension-underline'
import { useAdminDialog } from './AdminDialog'
import { useAdminLanguage } from '../AdminLanguageContext'

export function RichTextEditor({ value, onChange }) {
  const dialog = useAdminDialog()
  const { t } = useAdminLanguage()
  const editor = useEditor({
    editable: true,
    shouldRerenderOnTransaction: true,
    extensions: [
      StarterKit,
      Underline,
      Link.configure({
        openOnClick: false,
        autolink: false,
        HTMLAttributes: {
          target: '_blank',
          rel: 'noopener noreferrer',
        },
      }),
    ],
    content: value || '',
    editorProps: {
      attributes: {
        spellcheck: 'true',
        'aria-label': t('Article content'),
      },
    },
    onUpdate: ({ editor: current }) => {
      onChange(current.getHTML())
    },
  })

  if (!editor) return null

  const preserveSelection = command => event => {
    event.preventDefault()
    command()
  }

  const editLink = async event => {
    event.preventDefault()
    const selectionIsEmpty = editor.state.selection.empty
    const isInsideLink = editor.isActive('link')
    if (selectionIsEmpty && !isInsideLink) {
      editor.commands.focus()
      return
    }

    const url = await dialog.prompt({
      title: isInsideLink ? 'Edit link' : 'Add link',
      label: 'Link URL',
      defaultValue: editor.getAttributes('link').href ?? 'https://',
      confirmLabel: isInsideLink ? 'Update link' : 'Apply link',
      inputType: 'url',
    })

    if (url === null) {
      editor.commands.focus()
    } else if (!url.trim()) {
      editor.chain().focus().extendMarkRange('link').unsetLink().run()
    } else {
      const chain = editor.chain().focus()
      if (selectionIsEmpty && isInsideLink) chain.extendMarkRange('link')
      chain.setLink({ href: url.trim() }).run()
    }
  }

  return (
    <div className="rich-editor">
      <div className="rich-toolbar" role="toolbar" aria-label={t('Article formatting')}>
        <button type="button" aria-pressed={editor.isActive('bold')} className={editor.isActive('bold') ? 'active' : ''} onMouseDown={preserveSelection(() => editor.chain().focus().toggleBold().run())}><strong>B</strong></button>
        <button type="button" aria-pressed={editor.isActive('italic')} className={editor.isActive('italic') ? 'active' : ''} onMouseDown={preserveSelection(() => editor.chain().focus().toggleItalic().run())}><em>I</em></button>
        <button type="button" aria-pressed={editor.isActive('underline')} className={editor.isActive('underline') ? 'active' : ''} onMouseDown={preserveSelection(() => editor.chain().focus().toggleUnderline().run())}><u>U</u></button>
        <button type="button" aria-pressed={editor.isActive('heading', { level: 2 })} className={editor.isActive('heading', { level: 2 }) ? 'active' : ''} onMouseDown={preserveSelection(() => editor.chain().focus().toggleHeading({ level: 2 }).run())}>H2</button>
        <button type="button" aria-pressed={editor.isActive('bulletList')} className={editor.isActive('bulletList') ? 'active' : ''} onMouseDown={preserveSelection(() => editor.chain().focus().toggleBulletList().run())}>• List</button>
        <button type="button" aria-pressed={editor.isActive('orderedList')} className={editor.isActive('orderedList') ? 'active' : ''} onMouseDown={preserveSelection(() => editor.chain().focus().toggleOrderedList().run())}>1. List</button>
        <button type="button" aria-pressed={editor.isActive('link')} className={editor.isActive('link') ? 'active' : ''} onMouseDown={editLink}>Link</button>
        <button type="button" aria-pressed={editor.isActive('blockquote')} className={editor.isActive('blockquote') ? 'active' : ''} onMouseDown={preserveSelection(() => editor.chain().focus().toggleBlockquote().run())}>Quote</button>
        <span className="rich-toolbar-end">
          <button type="button" aria-label="Undo" title="Undo" disabled={!editor.can().undo()} onMouseDown={preserveSelection(() => editor.chain().focus().undo().run())}><span className="rich-symbol">↶</span></button>
          <button type="button" aria-label="Redo" title="Redo" disabled={!editor.can().redo()} onMouseDown={preserveSelection(() => editor.chain().focus().redo().run())}><span className="rich-symbol">↷</span></button>
          <button type="button" aria-label="Clear formatting" title="Clear formatting" onMouseDown={preserveSelection(() => editor.chain().focus().unsetAllMarks().clearNodes().run())}><span className="rich-symbol">⌫</span></button>
        </span>
      </div>
      <EditorContent editor={editor} />
    </div>
  )
}

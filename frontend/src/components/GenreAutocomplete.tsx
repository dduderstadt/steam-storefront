'use client';

import { useState, useRef, useEffect } from 'react';

interface GenreAutocompleteProps {
    genres: string[];
    selected: string[];
    onChange: (selected: string[]) => void;
}

/**
 * Multi-select autocomplete component for genre filtering.
 * Selected genres appear as removable chips inside the input.
 * Typing filters the remaining available genres; clicking a suggestion adds it.
 * Clicking x on a chip removes that genre from the selection.
 * Closes the dropdown when clicking outside.
 */
export default function GenreAutocomplete({ genres, selected, onChange }: GenreAutocompleteProps) {
    const [input, setInput] = useState(''); // Tracks user input in the textbox
    const [open, setOpen] = useState(false); // Controls dropdown visibility
    const ref = useRef<HTMLDivElement>(null);
    const [highlightedIndex, setHighlightedIndex] = useState(-1); // -1 means nothing highlighted

    // Derives the matching genres from the full list every render. Empty input = empty list (no dropdown with everything shown)
    // Case-insensitive prefix match - "ac" matches "Action"
    const filtered = input.length > 0 ? genres.filter(g => !selected.includes(g) && g.toLowerCase().startsWith(input.toLowerCase())) : genres.filter(g => !selected.includes(g));

    // Registers a mousedown listener on the document so clicking anywhere outside the
    // component closes the dropdown. Returns a cleanup function to remove the listener.
    // Without this, the listener would leak and fire after the component is gone.
    useEffect(() => {
        function handleClickOutside(e: MouseEvent) {
            if (ref.current && !ref.current.contains(e.target as Node)) {
                setOpen(false);
            }
        }
        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    /**
     * Called when the user clicks a genre in the dropdown. Adds the selected genre to the selection array and clears the text input.
     * Notifies the parent via onChange and closes the dropdown.
     * @param genre The selected genre from the auto-complete box.
     */
    function handleSelect(genre: string) {
        onChange([...selected, genre]);
        setInput('');
        setHighlightedIndex(-1); // Reset highlighted index when a genre is selected
        setOpen(false);
    }

    /**
     * Called when the use clicks the "x" on a selected genre chip.
     * @param genre The genre to be removed from the filter.
     */
    function handleRemove(genre: string) {
        onChange(selected.filter(g => g !== genre));
    }

    /**
     * Called on every keystroke. Updates the input, opens the dropdown.
     * @param e The change event from the input field.
     */
    function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
        setInput(e.target.value);
        setOpen(true);
    }

    /**
     * Handle arrow keyboard navigation and selection in the genres dropdown.
     * @param e The keyboard event from the input field.
     */
    function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        /**
         * The original early return (!open || filtered.length === 0) fires when the input is empty because an
         * empty input means no filtered results and the dropdown is closed.
         * Backspace is only useful when the input is already empty (to remove a chip), so
         * it ws being blocked before it could run.
         * 
         * Moving it first means backspace is checked regardless of the dropdown state -
         * empty input + backspace = remove a chip, no matter what else is happening.
         */
        if (e.key === 'Backspace' && input === '') {
            onChange(selected.slice(0, -1)); // Remove last selected genre
        }

        if (!open || filtered.length === 0) {
            return;
        }
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            setHighlightedIndex(i => Math.min(i + 1, filtered.length - 1));
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            setHighlightedIndex(i => Math.max(i - 1, -1));
        } else if (e.key === 'Enter' && highlightedIndex >= 0) {
            e.preventDefault();
            handleSelect(filtered[highlightedIndex]);
        } else if (e.key === 'Escape') {
            setOpen(false);
            setHighlightedIndex(-1);
        }
    }

    return (
        <div ref={ref} className="relative">
            <div className="flex flex-wrap gap-1 items-center border border-zinc-300 dark:border-zinc-700 rounded px-2 py-1 bg-white dark:bg-zinc-900 min-w-48">
                {selected.map(genre => (
                    <span key={genre} className="flex items-center gap-1 bg-zinc-100 dark:bg-zinc-800 text-zinc-700 dark:text-zinc-300 text-xs px-2 py-0.5 rounded">
                        {genre}
                        <button onClick={() => handleRemove(genre)} className="hover:text-zinc-900 dark:hover:text-zinc-100">&times;</button>
                    </span>
                ))}
                <input
                    type="text"
                    placeholder={selected.length === 0 ? 'Filter by genre...' : ''}
                    value={input}
                    onChange={handleChange}
                    onKeyDown={handleKeyDown}
                    onFocus={() => setOpen(true)}
                    className="flex-1 min-w-20 text-sm outline-none bg-transparent py-0.5" />
            </div>
            {open && filtered.length > 0 && (
                <ul className="absolute z-10 mt-1 w-full bg-white dark:bg-zinc-900 border border-zinc-300 dark:border-zinc-700 rounded shadow-lg max-h-48 overflow-y-auto">
                    {filtered.map((genre, index) => (
                        <li
                            key={genre}
                            onMouseDown={() => handleSelect(genre)}
                            className={`px-3 py-1.5 text-sm cursor-pointer ${index === highlightedIndex ? 'bg-zinc-200 dark:bg-zinc-700' : 'hover:bg-zinc-100 dark:hover:bg-zinc-800'}`}>
                            {genre}
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
'use client';

import { useState, useRef, useEffect } from 'react';

interface GenreAutocompleteProps {
    genres: string[];
    value: string;
    onChange: (value: string) => void;
}

/**
 * Renders an auto-complete text input for filtering by genre.
 * As the user types, it shows a dropdown of matching genres from the provided list.
 * When the user selects a genre, it calls onChange to notify the parent component.
 * It also handles closing the dropdown when clicking outside of it.
 */
export default function GenreAutocomplete({ genres, value, onChange }: GenreAutocompleteProps) {
    const [input, setInput] = useState(value); // Tracks user input in the textbox
    const [open, setOpen] = useState(false); // Controls dropdown visibility
    const ref = useRef<HTMLDivElement>(null);

    // Derives the matching genres from the full list every render. Empty input = empty list (no dropdown with everything shown)
    // Case-insensitive prefix match - "ac" matches "Action"
    const filtered = input.length > 0 ? genres.filter(g => g.toLowerCase().startsWith(input.toLowerCase())) : [];

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
     * Called when the user clicks a genre in the dropdown. Sets the input to the selected genre.
     * Notifies the parent via onChange and closes the dropdown.
     * @param genre The selected genre from the auto-complete box.
     */
    function handleSelect(genre: string) {
        setInput(genre);
        onChange(genre);
        setOpen(false);
    }

    /**
     * Called on every keystroke. Updates the input, opens the dropdown, and if the
     * input is cleared it also tells the parent the filter is gone.
     * @param e The change event from the input field.
     */
    function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
        setInput(e.target.value);
        setOpen(true);
        if (e.target.value === '') {
            onChange('');
        }
    }

    return (
        <div ref={ref} className="relative">
            <input
                type="text"
                placeholder="Filter by genre..."
                value={input}
                onChange={handleChange}
                onFocus={() => input.length > 0 && setOpen(true)} // Reopens the dropdown if you click back into a non-empty input
                className="border border-zinc-300 dark:border-zinc-700 rounded px-3 py-1.5 text-sm bg-white dark:bg-zinc-900 w-48"
            />
            {open && filtered.length > 0 && (
                <ul className="absolute z-10 mt-1 w-full bg-white dark:bg-zinc-900 border border-zinc-300 dark:border-zinc-700 rounded shadow-lg max-h-48 overflow-y-auto">
                    {filtered.map(genre => (
                        <li
                            key={genre}
                            onMouseDown={() => handleSelect(genre)}
                            className="px-3 py-1.5 text-sm cursor-pointer hover:bg-zinc-100 dark:hover:bg-zinc-800">
                            {genre}
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
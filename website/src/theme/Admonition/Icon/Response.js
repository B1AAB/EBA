import React from 'react';

// The mirrored counterpart of the prompt bubble (tail on the other side),
// holding a sparkle, i.e. a chatbot's generated answer.
export default function AdmonitionIconResponse(props) {
  return (
    <svg viewBox="0 0 16 16" {...props}>
      <g transform="translate(16 0) scale(-1 1)">
        <path
          fillRule="evenodd"
          d="M1.75 1h12.5c.966 0 1.75.784 1.75 1.75v9.5A1.75 1.75 0 0 1 14.25 14H8.061l-2.573 2.573A1.458 1.458 0 0 1 3 15.543V14H1.75A1.75 1.75 0 0 1 0 12.25v-9.5C0 1.784.784 1 1.75 1Zm-.25 1.75v9.5c0 .138.112.25.25.25h2a.75.75 0 0 1 .75.75v2.19l2.72-2.72a.75.75 0 0 1 .53-.22h6.5a.25.25 0 0 0 .25-.25v-9.5a.25.25 0 0 0-.25-.25H1.75a.25.25 0 0 0-.25.25Z"
        />
      </g>
      <path d="M8 4c.4 2.2 1.2 3 3.4 3.4-2.2.4-3 1.2-3.4 3.4-.4-2.2-1.2-3-3.4-3.4C6.8 7 7.6 6.2 8 4Z" />
    </svg>
  );
}

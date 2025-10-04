import React from 'react';
import clsx from 'clsx';


export default function Highlight({children, color}) {
  return (
    <span className={clsx('highlight', `highlight--${color}`)}>
      {children}
    </span>
  );
}
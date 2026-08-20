import DefaultAdmonitionTypes from '@theme-original/Admonition/Types';
import AdmonitionTypePrompt from './Type/Prompt';
import AdmonitionTypeResponse from './Type/Response';

// Custom admonition types. The keywords (`:::prompt`, `:::response`) are
// registered in docusaurus.config.js under the `admonitions` option.
export default {
  ...DefaultAdmonitionTypes,
  prompt: AdmonitionTypePrompt,
  response: AdmonitionTypeResponse,
};

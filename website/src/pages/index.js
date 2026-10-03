import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';
import HomepageHero from '@site/src/components/HomepageHero';

export default function Home() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title}`}
      description="Bitcoin's decade-long economy, now a temporal graph for machine learning">
      <HomepageHero />
      <main>
        <HomepageFeatures />
      </main>
    </Layout>
  );
}
